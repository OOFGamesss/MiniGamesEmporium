using System;
using System.Collections.Generic;
using System.Linq;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Actions;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Dispatches BAR 777 chat announcements in response to session events.</summary>

namespace MiniGamesEmporium.Games.Bar777.Automation;
public sealed class Bar777ChatAutomation : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly Bar777SessionService bar777SessionService;
    private readonly ChatQueueService chatQueue;
    private readonly HashSet<string> remindedPlayers = new(StringComparer.OrdinalIgnoreCase);

    public Bar777ChatAutomation(PluginConfiguration config, Bar777SessionService bar777SessionService, ChatQueueService chatQueue)
    {
        this.config = config;
        this.bar777SessionService = bar777SessionService;
        this.chatQueue = chatQueue;
        bar777SessionService.PaymentVerified  += OnPaymentVerified;
        bar777SessionService.HalfwayReached   += OnHalfwayReached;
        bar777SessionService.WinDetected      += OnWinDetected;
        bar777SessionService.SessionLost      += OnSessionLost;
        bar777SessionService.PlayerEnqueued   += OnPlayerEnqueued;
        bar777SessionService.SessionUpdated   += OnSessionUpdated;
    }

    public bool HasBeenReminded(string rawPlayerEntry) =>
        this.remindedPlayers.Contains(PlayerInfoService.StripWorld(rawPlayerEntry));

    public void SendManualReminder(string rawPlayerEntry, int displayOrdinal)
    {
        this.remindedPlayers.Add(PlayerInfoService.StripWorld(rawPlayerEntry));
        AnnounceReminderToPlay.Execute(WithWorld(rawPlayerEntry), displayOrdinal, this.config, this.chatQueue);
    }

    private void OnPaymentVerified(string playerName)
    {
        if (!this.config.Bar777.Chat.AutoStartRolls) return;
        AnnouncePaymentReceived.Execute(FullName(playerName), this.config, this.chatQueue);
    }

    private void OnHalfwayReached(string playerName, int rollsRemaining)
    {
        if (!this.config.Bar777.Chat.AutoSendHalfway) return;
        AnnounceHalfway.Execute(FullName(playerName), rollsRemaining, this.config, this.chatQueue);
    }

    private void OnWinDetected(string playerName, int rollValue)
    {
        if (!this.config.Bar777.Chat.AutoSendWinShout) return;
        var pot = this.config.Bar777.BoostedPot + this.config.Bar777.SessionTradedTotal;
        AnnounceWin.Execute(FullName(playerName), pot, this.config, this.chatQueue);
    }

    private void OnSessionLost(string playerName)
    {
        if (!this.config.Bar777.Chat.AutoSendUnlucky) return;
        AnnounceUnlucky.Execute(FullName(playerName), this.config, this.chatQueue);
    }

    private void OnPlayerEnqueued(string rawPlayerEntry)
    {
        var position = GetDisplayOrdinal(rawPlayerEntry);
        var becomingCurrent = this.config.QueuedPlayers.Count == 1
            && Bar777GameIds.IsWaitingPlaceholder(this.config.ActiveSession?.PlayerName);
        if (becomingCurrent && this.config.Bar777.Chat.AutoSendReminderToPlay)
        {
            this.remindedPlayers.Add(PlayerInfoService.StripWorld(rawPlayerEntry));
            AnnounceReminderToPlay.Execute(WithWorld(rawPlayerEntry), position, this.config, this.chatQueue);
            return;
        }
        if (!this.config.Bar777.Chat.AutoSendJoinQueue) return;
        var willReceiveReminder = this.config.Bar777.Chat.AutoSendReminderToPlay
            && position <= this.config.Bar777.Chat.ReminderQueueThreshold;
        if (willReceiveReminder) return;
        AnnounceJoinQueue.Execute(WithWorld(rawPlayerEntry), position, this.config, this.chatQueue);
    }

    private int GetDisplayOrdinal(string rawPlayerEntry)
    {
        var queue = this.config.QueuedPlayers;
        var currentName = this.config.ActiveSession?.PlayerName?.Trim();
        var hasActiveCurrent = !string.IsNullOrEmpty(currentName) && !Bar777GameIds.IsAnyPlaceholder(currentName);
        var targetName = PlayerInfoService.StripWorld(rawPlayerEntry);
        var ordinal = hasActiveCurrent ? 1 : 0;
        foreach (var entry in queue)
        {
            var name = PlayerInfoService.StripWorld(entry);
            if (hasActiveCurrent && name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                continue;
            ordinal++;
            if (name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return ordinal;
        }
        return ordinal;
    }

    private void OnSessionUpdated()
    {
        var queue = this.config.QueuedPlayers;
        var queuedNames = new HashSet<string>(queue.Select(PlayerInfoService.StripWorld), StringComparer.OrdinalIgnoreCase);
        this.remindedPlayers.RemoveWhere(name => !queuedNames.Contains(name));
        if (!this.config.Bar777.Chat.AutoSendReminderToPlay) return;
        if (!this.config.Bar777.UseQueue) return;
        var threshold = this.config.Bar777.Chat.ReminderQueueThreshold;
        if (threshold <= 0) return;
        var currentName = this.config.ActiveSession?.PlayerName?.Trim();
        var hasActiveCurrent = !string.IsNullOrEmpty(currentName) && !Bar777GameIds.IsAnyPlaceholder(currentName);
        var displayOrdinal = hasActiveCurrent ? 1 : 0;
        foreach (var entry in queue)
        {
            var name = PlayerInfoService.StripWorld(entry);
            if (hasActiveCurrent && name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                continue;
            displayOrdinal++;
            if (displayOrdinal <= threshold && !this.remindedPlayers.Contains(name))
            {
                this.remindedPlayers.Add(name);
                AnnounceReminderToPlay.Execute(WithWorld(entry), displayOrdinal, this.config, this.chatQueue);
            }
        }
    }

    private string FullName(string playerName)
    {
        var world = this.config.ActiveSession?.PlayerWorld;
        var entry = string.IsNullOrEmpty(world) ? playerName : $"{playerName}@{world}";
        return WithWorld(entry);
    }

    private static string WithWorld(string rawEntry)
    {
        if (rawEntry.Contains('@')) return rawEntry;
        var name = rawEntry.Trim();
        var player = PlayerInfoService.FindNearby(name);
        if (player == null) return rawEntry;
        var world = player.HomeWorld.Value.Name.ToString();
        return string.IsNullOrEmpty(world) ? rawEntry : $"{name}@{world}";
    }

    public void Dispose()
    {
        this.bar777SessionService.PaymentVerified  -= OnPaymentVerified;
        this.bar777SessionService.HalfwayReached   -= OnHalfwayReached;
        this.bar777SessionService.WinDetected      -= OnWinDetected;
        this.bar777SessionService.SessionLost      -= OnSessionLost;
        this.bar777SessionService.PlayerEnqueued   -= OnPlayerEnqueued;
        this.bar777SessionService.SessionUpdated   -= OnSessionUpdated;
    }
}
