using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.GameHelpers;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.Raffle.Utility;
using System;

/// <summary>Forwards completed gil trades to the game services for payment verification.</summary>

namespace MiniGamesEmporium.Services;
public sealed class TradeListenerService : IDisposable
{
    private readonly Bar777SessionService bar777SessionService;
    private readonly SessionService sessionService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly DeathrollBettingService deathrollBettingService;
    private readonly HigherLowerService higherLowerService;
    private readonly RaffleService raffleService;
    private readonly CoinCollectorService coinCollectorService;
    private readonly IPluginLog log;

    public TradeListenerService(Bar777SessionService bar777SessionService, SessionService sessionService, DeathrollTournamentService deathrollService, DeathrollBettingService deathrollBettingService, HigherLowerService higherLowerService, RaffleService raffleService, CoinCollectorService coinCollectorService, IPluginLog log)
    {
        this.bar777SessionService     = bar777SessionService;
        this.sessionService           = sessionService;
        this.deathrollService         = deathrollService;
        this.deathrollBettingService  = deathrollBettingService;
        this.higherLowerService       = higherLowerService;
        this.raffleService            = raffleService;
        this.coinCollectorService     = coinCollectorService;
        this.log                      = log;
        TradeDetectionManager.OnTradeEnd += OnTradeEnd;
    }

    public void Dispose()
    {
        TradeDetectionManager.OnTradeEnd -= OnTradeEnd;
    }

    private void OnTradeEnd(IPlayerCharacter? counterparty, TradeDetectionManager.TradeDescriptor? result)
    {
        if (result == null || counterparty == null) return;
        var name  = counterparty.Name.TextValue;
        var world = counterparty.HomeWorld.Value.Name.ToString();
        if (result.ReceivedGil > 0)
        {
            var focused = this.sessionService.FocusedGameName;
            if (focused == null) return;
            this.log.Information($"Trade complete: {name}@{world} gave {result.ReceivedGil:N0} gil to {focused}.");
            if (Bar777GameIds.Matches(focused))
            {
                this.bar777SessionService.VerifyPayment(name, result.ReceivedGil, world);
            }
            else if (DeathrollGameIds.Matches(focused))
            {
                this.deathrollService.TryAutoMarkPaid(name, result.ReceivedGil);
                this.deathrollBettingService.TryAutoApplyBetTrade(name, result.ReceivedGil);
            }
            else if (HigherLowerGameIds.Matches(focused))
            {
                this.higherLowerService.TryVerifyPayment(name, result.ReceivedGil, world);
            }
            else if (RaffleGameIds.Matches(focused))
            {
                this.raffleService.TryAutoAddTickets(name, result.ReceivedGil, world);
            }
            else if (CoinCollectorGameIds.Matches(focused))
            {
                this.coinCollectorService.TryVerifyPayment(name, result.ReceivedGil, world);
            }
        }
        else if (result.ReceivedGil < 0)
        {
            var amountSent = (long)(-result.ReceivedGil);
            this.log.Information($"Payout trade: sent {amountSent:N0} gil to {name}@{world}.");
            this.bar777SessionService.TryRecordWinnerPayout(name, amountSent);
            this.deathrollService.TryRecordWinnerPayout(name, amountSent);
            this.deathrollBettingService.TryRecordBetPayout(name, amountSent);
            this.higherLowerService.TryRecordWinnerPayout(name, amountSent);
            this.raffleService.TryRecordWinnerPayout(name, amountSent);
            this.coinCollectorService.TryRecordWinnerPayout(name, amountSent);
        }
    }
}
