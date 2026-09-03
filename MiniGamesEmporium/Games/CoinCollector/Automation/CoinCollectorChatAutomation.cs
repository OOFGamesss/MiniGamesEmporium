using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Actions;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Services;

/// <summary>Dispatches Coin Collector roll prompts and score announcements in response to session events.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Automation;
public sealed class CoinCollectorChatAutomation : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly CoinCollectorService coinCollectorService;
    private readonly ChatQueueService chatQueue;

    public CoinCollectorChatAutomation(PluginConfiguration config, CoinCollectorService coinCollectorService, ChatQueueService chatQueue)
    {
        this.config               = config;
        this.coinCollectorService = coinCollectorService;
        this.chatQueue            = chatQueue;
        coinCollectorService.SessionLost      += OnSessionLost;
        coinCollectorService.RollAwaitingNext += OnRollAwaitingNext;
        coinCollectorService.WrongRollDetected += OnWrongRollDetected;
    }

    private void OnWrongRollDetected(int rollValue, int wrongRollMax, int expectedRollMax)
    {
        if (!this.config.CoinCollector.Chat.AutoSendWrongRoll) return;
        var session = this.coinCollectorService.GetActiveSession();
        if (session == null) return;
        AnnounceWrongRoll.Execute(FullName(session.PlayerName), wrongRollMax, expectedRollMax, this.config, this.chatQueue);
    }

    private void OnSessionLost(string playerName, int coins)
    {
        if (!this.config.CoinCollector.Chat.AutoSendLoss) return;
        var fullName  = FullName(playerName);
        var isLeading = this.coinCollectorService.IsCurrentlyLeading(coins);
        var target    = this.coinCollectorService.GetLeadTarget(coins);
        AnnounceCoinCollectorLoss.ExecuteLeaderboardAnnounce(fullName, coins, isLeading, target, this.config, this.chatQueue);
    }

    private void OnRollAwaitingNext(int nextRollMax, int coins)
    {
        if (!this.config.CoinCollector.Chat.AutoSendAskRoll) return;
        var session  = this.coinCollectorService.GetActiveSession();
        var fullName = FullName(session?.PlayerName ?? string.Empty);
        AnnounceAskRoll.Execute(fullName, nextRollMax, coins, this.config, this.chatQueue);
    }

    private string FullName(string playerName)
    {
        var world = this.coinCollectorService.GetActiveSession()?.PlayerWorld;
        return string.IsNullOrEmpty(world) ? playerName : $"{playerName}@{world}";
    }

    public void Dispose()
    {
        this.coinCollectorService.SessionLost      -= OnSessionLost;
        this.coinCollectorService.RollAwaitingNext -= OnRollAwaitingNext;
        this.coinCollectorService.WrongRollDetected -= OnWrongRollDetected;
    }
}
