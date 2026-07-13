using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.GameHelpers;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.Raffle.Services;
using System;

/// <summary>Forwards completed gil trades to the game services for payment verification.</summary>

namespace MiniGamesEmporium.Services;
public sealed class TradeListenerService : IDisposable
{
    private readonly Bar777SessionService bar777SessionService;
    private readonly SessionService sessionService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly HigherLowerService higherLowerService;
    private readonly RaffleService raffleService;
    private readonly IPluginLog log;

    public TradeListenerService(Bar777SessionService bar777SessionService, SessionService sessionService, DeathrollTournamentService deathrollService, HigherLowerService higherLowerService, RaffleService raffleService, IPluginLog log)
    {
        this.bar777SessionService   = bar777SessionService;
        this.sessionService   = sessionService;
        this.deathrollService = deathrollService;
        this.higherLowerService = higherLowerService;
        this.raffleService    = raffleService;
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
        var hasBar777Session = this.bar777SessionService.IsSessionActive();
        var hasDRSession     = this.deathrollService.IsSessionActive();
        var hasHLSession     = this.higherLowerService.IsSessionActive();
        var hasRaffleSession = this.raffleService.IsSessionActive();
        if (!hasBar777Session && !hasDRSession && !hasHLSession && !hasRaffleSession) return;

        var name  = counterparty.Name.TextValue;
        var world = counterparty.HomeWorld.Value.Name.ToString();
        if (result.ReceivedGil > 0)
        {
            if (this.sessionService.IsPaused) return;
            this.log.Information($"Trade complete: {name}@{world} gave {result.ReceivedGil:N0} gil.");
            this.bar777SessionService.VerifyPayment(name, result.ReceivedGil, world);
            this.deathrollService.TryAutoMarkPaid(name, result.ReceivedGil);
            this.higherLowerService.TryVerifyPayment(name, result.ReceivedGil, world);
            this.raffleService.TryAutoAddTickets(name, result.ReceivedGil, world);
        }
        else if (result.ReceivedGil < 0)
        {
            var amountSent = (long)(-result.ReceivedGil);
            this.log.Information($"Payout trade: sent {amountSent:N0} gil to {name}@{world}.");
            this.bar777SessionService.TryRecordWinnerPayout(name, amountSent);
            this.deathrollService.TryRecordWinnerPayout(name, amountSent);
            this.higherLowerService.TryRecordWinnerPayout(name, amountSent);
            this.raffleService.TryRecordWinnerPayout(name, amountSent);
        }
    }
}
