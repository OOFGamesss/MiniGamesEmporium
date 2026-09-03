using System;
using ECommons.Automation.NeoTaskManager;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Actions;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Services;

/// <summary>Drives Coin Collector turn progression so the host does not have to click through each stage.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Automation;
public sealed class CoinCollectorTurnAutomation : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly CoinCollectorService coinCollectorService;
    private readonly ChatQueueService chatQueue;
    private readonly TaskManager taskManager = new();

    public CoinCollectorTurnAutomation(PluginConfiguration config, CoinCollectorService coinCollectorService, ChatQueueService chatQueue)
    {
        this.config               = config;
        this.coinCollectorService = coinCollectorService;
        this.chatQueue            = chatQueue;
        this.coinCollectorService.PaymentReady += OnPaymentReady;
        this.coinCollectorService.SessionLost  += OnTurnFinished;
        this.coinCollectorService.WinDetected  += OnTurnFinished;
    }

    private void OnPaymentReady(string playerName, int attempts)
    {
        if (!this.config.CoinCollector.AutoBeginOnPayment) return;
        var session = this.coinCollectorService.GetActiveSession();
        if (session == null || session.PaymentVerified) return;

        this.coinCollectorService.StartGame();
        if (!this.config.CoinCollector.Chat.AutoSendPaymentReceived) return;
        var rollMax = this.coinCollectorService.GetNextRollCommandMax();
        AnnouncePaymentReceived.Execute(FullName(playerName), rollMax, attempts, this.config, this.chatQueue);
    }

    private void OnTurnFinished(string playerName, int coins)
    {
        var cc = this.config.CoinCollector;
        if (!cc.AutoEndTurn) return;

        this.taskManager.EnqueueDelay(Math.Max(0, cc.AutoEndTurnDelayMs));
        this.taskManager.Enqueue(AdvanceIfStillFinished);
    }

    private void AdvanceIfStillFinished()
    {
        if (!this.config.CoinCollector.AutoEndTurn) return;
        var turn = this.coinCollectorService.GetActiveTurn();
        if (turn == null || !turn.IsGameOver) return;
        this.coinCollectorService.AdvanceAfterTurn();
    }

    private string FullName(string playerName)
    {
        var world = this.coinCollectorService.GetActiveSession()?.PlayerWorld;
        return string.IsNullOrEmpty(world) ? playerName : $"{playerName}@{world}";
    }

    public void Dispose()
    {
        this.coinCollectorService.PaymentReady -= OnPaymentReady;
        this.coinCollectorService.SessionLost  -= OnTurnFinished;
        this.coinCollectorService.WinDetected  -= OnTurnFinished;
        this.taskManager.Dispose();
    }
}
