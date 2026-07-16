using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Manages side bets on the tournament winner declared during registration.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Services;
public sealed class DeathrollBettingService : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly HistoryService historyService;
    private readonly DeathrollTournamentService deathrollService;
    public event Action? SessionUpdated;
    public event Action<string, long, List<string>>? BetPayoutsResolved;

    public DeathrollBettingService(PluginConfiguration config, HistoryService historyService, DeathrollTournamentService deathrollService)
    {
        this.config            = config;
        this.historyService    = historyService;
        this.deathrollService  = deathrollService;
        this.deathrollService.TournamentStarted += OnTournamentStarted;
        this.deathrollService.TournamentWon     += OnTournamentWon;
    }

    public void Dispose()
    {
        this.deathrollService.TournamentStarted -= OnTournamentStarted;
        this.deathrollService.TournamentWon     -= OnTournamentWon;
    }

    public bool IsBettingEnabledForSession() =>
        this.config.DeathrollSession?.BettingEnabled ?? this.config.DeathrollTournament.BettingEnabled;

    public long GetBetUnit()
    {
        var state = this.deathrollService.GetState();
        if (state != null) return state.BetUnitAtStart;
        return this.config.DeathrollSession?.BetUnit ?? this.config.DeathrollTournament.BetUnit;
    }

    public void DeclareBet(string bettorEntry, string targetEntry, bool fromChat)
    {
        if (!CanDeclare()) return;
        if (string.IsNullOrWhiteSpace(bettorEntry) || string.IsNullOrWhiteSpace(targetEntry)) return;
        var bets = this.config.DeathrollTournament.Bets;
        if (bets.Any(b => !b.NeedsReview
                        && DeathrollTournamentService.NamesMatch(b.BettorName, bettorEntry)
                        && DeathrollTournamentService.NamesMatch(b.TargetName, targetEntry)))
            return;
        bets.Add(new DeathrollBet
        {
            BettorName = bettorEntry.Trim(),
            TargetName = targetEntry.Trim(),
            FromChat   = fromChat,
        });
        Save();
        SessionUpdated?.Invoke();
    }

    public void DeclareUnresolvedBet(string bettorEntry, string rawTargetText)
    {
        if (!CanDeclare()) return;
        if (string.IsNullOrWhiteSpace(bettorEntry)) return;
        this.config.DeathrollTournament.Bets.Add(new DeathrollBet
        {
            BettorName  = bettorEntry.Trim(),
            TargetName  = rawTargetText.Trim(),
            NeedsReview = true,
            FromChat    = true,
        });
        Save();
        SessionUpdated?.Invoke();
    }

    public void CorrectBetTarget(Guid betId, string newTargetEntry)
    {
        if (string.IsNullOrWhiteSpace(newTargetEntry)) return;
        var bets = this.config.DeathrollTournament.Bets;
        var bet  = bets.FirstOrDefault(b => b.Id == betId);
        if (bet == null) return;
        if (bets.Any(b => b.Id != betId
                        && !b.NeedsReview
                        && DeathrollTournamentService.NamesMatch(b.BettorName, bet.BettorName)
                        && DeathrollTournamentService.NamesMatch(b.TargetName, newTargetEntry)))
        {
            bets.Remove(bet);
            Save();
            SessionUpdated?.Invoke();
            return;
        }
        bet.TargetName  = newTargetEntry.Trim();
        bet.NeedsReview = false;
        Save();
        SessionUpdated?.Invoke();
    }

    public void SetBetPaid(Guid betId, bool paid)
    {
        var bet = this.config.DeathrollTournament.Bets.FirstOrDefault(b => b.Id == betId);
        if (bet == null || bet.IsPaid == paid) return;
        bet.IsPaid = paid;
        Save();
        SessionUpdated?.Invoke();
    }

    public void RemoveBet(Guid betId)
    {
        var bets = this.config.DeathrollTournament.Bets;
        var bet  = bets.FirstOrDefault(b => b.Id == betId);
        if (bet == null) return;
        bets.Remove(bet);
        Save();
        SessionUpdated?.Invoke();
    }

    public void TryAutoApplyBetTrade(string tradePartner, long gilReceived)
    {
        if (!this.deathrollService.IsSessionActive() || this.deathrollService.HasActiveTournament()) return;
        if (!IsBettingEnabledForSession()) return;
        var betUnit = GetBetUnit();
        if (betUnit <= 0 || gilReceived <= 0) return;

        var reserved          = this.deathrollService.GetEntryCostReservation(tradePartner, gilReceived);
        var availableForBets  = gilReceived - reserved;
        if (availableForBets <= 0) return;

        var pending = this.config.DeathrollTournament.Bets
            .Where(b => !b.IsPaid && !b.NeedsReview && DeathrollTournamentService.NamesMatch(b.BettorName, tradePartner))
            .OrderBy(b => b.DeclaredAt)
            .ToList();
        if (pending.Count == 0) return;

        var key           = PlayerInfoService.StripWorld(tradePartner);
        var credit        = this.config.DeathrollTournament.BettorCredit;
        var currentCredit = credit.TryGetValue(key, out var c) ? c : 0L;
        var available     = currentCredit + availableForBets;
        var wanted        = available / betUnit;
        var granted       = (int)Math.Min(wanted, pending.Count);
        for (var i = 0; i < granted; i++)
            pending[i].IsPaid = true;
        credit[key] = available - (long)granted * betUnit;

        this.historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = $"{PlayerInfoService.StripWorld(tradePartner)} (Bet)",
            Amount     = (int)Math.Min(availableForBets, int.MaxValue),
            Timestamp  = DateTime.UtcNow,
            GameName   = DeathrollGameIds.DisplayName,
        });
        Save();
        SessionUpdated?.Invoke();
    }

    private long ComputeBettingPotPreview()
    {
        var betUnit = GetBetUnit();
        if (betUnit <= 0) return 0;
        return betUnit * ComputeConfirmedBetCountPreview();
    }

    public long ComputeBettingPot()
    {
        var state = this.deathrollService.GetState();
        return state != null ? state.BettingPotAtStart : ComputeBettingPotPreview();
    }

    private int ComputeConfirmedBetCountPreview()
    {
        var registered = this.config.DeathrollTournament.RegisteredPlayers;
        return this.config.DeathrollTournament.Bets
            .Count(b => b.IsPaid && !b.NeedsReview && registered.Any(p => DeathrollTournamentService.NamesMatch(p, b.TargetName)));
    }

    public int ComputeConfirmedBetCount()
    {
        var state = this.deathrollService.GetState();
        return state != null ? state.ConfirmedBets.Count : ComputeConfirmedBetCountPreview();
    }

    public List<DeathrollBet> GetUnresolvedBets()
    {
        var registered = this.config.DeathrollTournament.RegisteredPlayers;
        return this.config.DeathrollTournament.Bets
            .Where(b => b.NeedsReview || !b.IsPaid || !registered.Any(p => DeathrollTournamentService.NamesMatch(p, b.TargetName)))
            .ToList();
    }

    public void TryRecordBetPayout(string partnerName, long amountSent)
    {
        var state  = this.deathrollService.GetState();
        var payout = state?.BetPayouts.FirstOrDefault(p => DeathrollTournamentService.NamesMatch(p.BettorName, partnerName));
        if (payout == null) return;
        payout.PaidGil += amountSent;
        payout.PayoutTransactionId = PayoutTransactionRecorder.Record(
            this.historyService, DeathrollGameIds.DisplayName, payout.BettorName, payout.PaidGil, payout.PayoutTransactionId, "Bet Payout");
        Save();
        SessionUpdated?.Invoke();
    }

    private bool CanDeclare() =>
        this.deathrollService.IsSessionActive() && !this.deathrollService.HasActiveTournament() && IsBettingEnabledForSession();

    private void OnTournamentStarted(DeathrollTournamentState state)
    {
        var cfg = this.config.DeathrollTournament;
        state.BettingEnabledAtStart = this.config.DeathrollSession?.BettingEnabled ?? cfg.BettingEnabled;
        state.BetUnitAtStart        = this.config.DeathrollSession?.BetUnit ?? cfg.BetUnit;
        if (!state.BettingEnabledAtStart || state.Rounds.Count == 0) return;

        var finalEntrants = state.Rounds[0]
            .SelectMany(m => new[] { m.Player1, m.Player2 })
            .Where(p => !string.IsNullOrEmpty(p) && !DeathrollGameIds.IsBye(p))
            .ToList();
        foreach (var bet in cfg.Bets.Where(b => b.IsPaid && !b.NeedsReview))
        {
            if (finalEntrants.Any(p => DeathrollTournamentService.NamesMatch(p, bet.TargetName)))
                state.ConfirmedBets.Add(new DeathrollBetSnapshot { BettorName = bet.BettorName, TargetName = bet.TargetName });
        }
        state.BettingPotAtStart = state.BetUnitAtStart * state.ConfirmedBets.Count;
    }

    private void OnTournamentWon(string winner, long totalPot)
    {
        var state = this.deathrollService.GetState();
        if (state == null || !state.BettingEnabledAtStart || state.BetPayouts.Count > 0) return;
        var winners = state.ConfirmedBets
            .Where(b => DeathrollTournamentService.NamesMatch(b.TargetName, winner))
            .Select(b => b.BettorName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (winners.Count == 0) return;
        var share = state.BettingPotAtStart / winners.Count;
        foreach (var name in winners)
            state.BetPayouts.Add(new DeathrollBetPayout { BettorName = name, ShareGil = share });
        Save();
        SessionUpdated?.Invoke();
        BetPayoutsResolved?.Invoke(winner, state.BettingPotAtStart, winners);
    }

    private void Save() => this.config.Save();
}
