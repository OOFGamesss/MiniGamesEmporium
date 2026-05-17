using Dalamud.Game.ClientState.Objects.SubKinds;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Actions;
using MiniGamesEmporium.Services;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Subscribes to BAR 777 session events and automatically dispatches the appropriate chat announcements, including payment confirmation, halfway notices, win shouts, unlucky messages, queue confirmations, and play reminders.</summary>

namespace MiniGamesEmporium.Games.Bar777.Automation;
public sealed class Bar777ChatAutomation : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly ChatQueueService chatQueue;
    private readonly HashSet<string> remindedPlayers = new(StringComparer.OrdinalIgnoreCase);
    public Bar777ChatAutomation(PluginConfiguration config, SessionService sessionService, ChatQueueService chatQueue)
    {
        this.config = config;
        this.sessionService = sessionService;
        this.chatQueue = chatQueue;
        sessionService.PaymentVerified  += OnPaymentVerified;
        sessionService.HalfwayReached   += OnHalfwayReached;
        sessionService.WinDetected      += OnWinDetected;
        sessionService.SessionLost      += OnSessionLost;
        sessionService.PlayerEnqueued   += OnPlayerEnqueued;
        sessionService.SessionUpdated   += OnSessionUpdated;
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
            this.remindedPlayers.Add(ParseName(rawPlayerEntry));
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
        var targetName = ParseName(rawPlayerEntry);
        var ordinal = hasActiveCurrent ? 1 : 0;
        foreach (var entry in queue)
        {
            var name = ParseName(entry);
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
        var queuedNames = new HashSet<string>(queue.Select(ParseName), StringComparer.OrdinalIgnoreCase);
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
            var name = ParseName(entry);
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
    public bool HasBeenReminded(string rawPlayerEntry) =>
        this.remindedPlayers.Contains(ParseName(rawPlayerEntry));
    public void SendManualReminder(string rawPlayerEntry, int displayOrdinal)
    {
        this.remindedPlayers.Add(ParseName(rawPlayerEntry));
        AnnounceReminderToPlay.Execute(WithWorld(rawPlayerEntry), displayOrdinal, this.config, this.chatQueue);
    }
    private string FullName(string playerName)
    {
        var world = this.config.ActiveSession?.PlayerWorld;
        var entry = string.IsNullOrEmpty(world) ? playerName : $"{playerName}@{world}";
        return WithWorld(entry);
    }
    private static string ParseName(string raw)
    {
        var at = raw.IndexOf('@');
        return at < 0 ? raw.Trim() : raw[..at].Trim();
    }
    private static string WithWorld(string rawEntry)
    {
        if (rawEntry.Contains('@')) return rawEntry;
        var name = rawEntry.Trim();
        var player = MiniGamesEmporium.ObjectTable
            .OfType<IPlayerCharacter>()
            .FirstOrDefault(p => p.Name.TextValue.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (player == null) return rawEntry;
        var world = player.HomeWorld.Value.Name.ToString();
        return string.IsNullOrEmpty(world) ? rawEntry : $"{name}@{world}";
    }
    public void Dispose()
    {
        this.sessionService.PaymentVerified  -= OnPaymentVerified;
        this.sessionService.HalfwayReached   -= OnHalfwayReached;
        this.sessionService.WinDetected      -= OnWinDetected;
        this.sessionService.SessionLost      -= OnSessionLost;
        this.sessionService.PlayerEnqueued   -= OnPlayerEnqueued;
        this.sessionService.SessionUpdated   -= OnSessionUpdated;
    }
}
