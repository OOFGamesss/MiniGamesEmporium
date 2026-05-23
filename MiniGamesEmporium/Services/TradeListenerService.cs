using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.GameHelpers;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using System;

/// <summary>Subscribes to the ECommons trade detection event and forwards completed gil receipts to the session and deathroll services for payment verification.</summary>

namespace MiniGamesEmporium.Services;
public sealed class TradeListenerService : IDisposable
{
    private readonly SessionService sessionService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly IPluginLog log;

    public TradeListenerService(SessionService sessionService, DeathrollTournamentService deathrollService, IPluginLog log)
    {
        this.sessionService   = sessionService;
        this.deathrollService = deathrollService;
        this.log              = log;
        TradeDetectionManager.OnTradeEnd += OnTradeEnd;
    }

    public void Dispose()
    {
        TradeDetectionManager.OnTradeEnd -= OnTradeEnd;
    }

    private void OnTradeEnd(IPlayerCharacter? counterparty, TradeDetectionManager.TradeDescriptor? result)
    {
        if (this.sessionService.IsPaused) return;
        if (result == null || result.ReceivedGil <= 0 || counterparty == null) return;
        var name  = counterparty.Name.TextValue;
        var world = counterparty.HomeWorld.Value.Name.ToString();
        this.log.Information($"[MGE] Trade complete: {name}@{world} gave {result.ReceivedGil:N0} gil.");
        this.sessionService.VerifyPayment(name, result.ReceivedGil, world);
        this.deathrollService.TryAutoMarkPaid(name, result.ReceivedGil);
    }
}
