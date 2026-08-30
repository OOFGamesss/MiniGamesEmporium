using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Actions;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Services;

/// <summary>Dispatches Higher/Lower win and loss announcements in response to session events.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Automation;
public sealed class HigherLowerChatAutomation : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly HigherLowerService higherLowerService;
    private readonly ChatQueueService chatQueue;

    public HigherLowerChatAutomation(PluginConfiguration config, HigherLowerService higherLowerService, ChatQueueService chatQueue)
    {
        this.config             = config;
        this.higherLowerService = higherLowerService;
        this.chatQueue          = chatQueue;
        higherLowerService.SessionLost        += OnSessionLost;
        higherLowerService.PaymentVerified    += OnPaymentVerified;
        higherLowerService.RollAwaitingGuess  += OnRollAwaitingGuess;
    }

    private void OnSessionLost(string playerName, int rounds)
    {
        if (!this.config.HigherLower.Chat.AutoSendLoss) return;
        var fullName  = FullName(playerName);
        var isLeading = this.higherLowerService.IsCurrentlyLeading(rounds);
        var target    = this.higherLowerService.GetLeadTarget(rounds);
        AnnounceHigherLowerLoss.ExecuteLeaderboardAnnounce(fullName, rounds, isLeading, target, this.config, this.chatQueue);
    }

    private void OnPaymentVerified(string playerName)
    {
        if (!this.config.HigherLower.Chat.AutoSendLetsPlay) return;
        AnnounceLetsPlay.Execute(FullName(playerName), this.config, this.chatQueue);
    }

    private void OnRollAwaitingGuess(int rolledNumber)
    {
        if (!this.config.HigherLower.Chat.AutoSendAskGuess) return;
        var session  = this.higherLowerService.GetActiveSession();
        var fullName = FullName(session?.PlayerName ?? string.Empty);
        AnnounceAskGuess.Execute(fullName, rolledNumber, this.config, this.chatQueue);
    }

    private string FullName(string playerName)
    {
        var world = this.higherLowerService.GetActiveSession()?.PlayerWorld;
        return string.IsNullOrEmpty(world) ? playerName : $"{playerName}@{world}";
    }

    public void Dispose()
    {
        this.higherLowerService.SessionLost       -= OnSessionLost;
        this.higherLowerService.PaymentVerified   -= OnPaymentVerified;
        this.higherLowerService.RollAwaitingGuess -= OnRollAwaitingGuess;
    }
}
