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
        if (result == null || counterparty == null) return;
        var hasBar777Session = this.sessionService.GetActiveSession() != null;
        var hasDRSession     = this.deathrollService.IsSessionActive();
        if (!hasBar777Session && !hasDRSession) return;

        var name  = counterparty.Name.TextValue;
        var world = counterparty.HomeWorld.Value.Name.ToString();
        if (result.ReceivedGil > 0)
        {
            if (this.sessionService.IsPaused) return;
            this.log.Information($"Trade complete: {name}@{world} gave {result.ReceivedGil:N0} gil.");
            this.sessionService.VerifyPayment(name, result.ReceivedGil, world);
            this.deathrollService.TryAutoMarkPaid(name, result.ReceivedGil);
        }
        else if (result.ReceivedGil < 0)
        {
            var amountSent = (long)(-result.ReceivedGil);
            this.log.Information($"Payout trade: sent {amountSent:N0} gil to {name}@{world}.");
            this.sessionService.TryRecordWinnerPayout(name, amountSent);
            this.deathrollService.TryRecordWinnerPayout(name, amountSent);
        }
    }
}
