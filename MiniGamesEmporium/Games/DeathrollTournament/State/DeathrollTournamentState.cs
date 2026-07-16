using System;
using System.Collections.Generic;
using MiniGamesEmporium.Games.DeathrollTournament.Models;

/// <summary>Serialisable snapshot of a live Deathroll Tournament session and its match phases.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.State;

public enum MatchPhase
{
    NotStarted,
    DeterminingOrder,
    Deathrolling,
    GameOver,
    MatchComplete,
}

[Serializable]
public class DeathrollTournamentState
{
    public string GameName { get; set; } = "Deathroll Tournament";
    public long EntryCostAtStart { get; set; } = 0;
    public long BoostedPotAtStart { get; set; } = 0;
    public int PlayerCountAtStart { get; set; } = 0;
    public long PotAdjustment { get; set; } = 0L;
    public DeathrollPrizeType PrizeTypeAtStart { get; set; } = DeathrollPrizeType.Gil;
    public string PrizeItemNameAtStart { get; set; } = string.Empty;
    public string PrizeCustomTextAtStart { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public List<List<BracketMatch>> Rounds { get; set; } = new();
    public List<int> BestOfPerRound { get; set; } = new();

    public int CurrentRoundIndex { get; set; } = 0;
    public int CurrentMatchIndex { get; set; } = 0;

    public int OrderRollPlayer1 { get; set; } = 0;
    public int OrderRollPlayer2 { get; set; } = 0;
    public int LastOrderTiedValue { get; set; } = 0;

    public string CurrentTurnPlayerName { get; set; } = string.Empty;
    public int CurrentDeathrollMax { get; set; } = 0;
    public List<DeathrollEntry> ActiveRollLog { get; set; } = new();

    public int ActiveMatchPlayer1Wins { get; set; } = 0;
    public int ActiveMatchPlayer2Wins { get; set; } = 0;
    public MatchPhase ActiveMatchPhase { get; set; } = MatchPhase.NotStarted;

    public string? TournamentWinner { get; set; } = null;
    public long WinnerPayoutGil { get; set; } = 0;
    public Guid? PayoutTransactionId { get; set; } = null;

    public bool BettingEnabledAtStart { get; set; } = false;
    public long BetUnitAtStart { get; set; } = 0;
    public List<DeathrollBetSnapshot> ConfirmedBets { get; set; } = new();
    public long BettingPotAtStart { get; set; } = 0;
    public List<DeathrollBetPayout> BetPayouts { get; set; } = new();

    public string CurrentRoundLabel() =>
        CurrentRoundIndex == Rounds.Count - 1 ? "The Final" : $"Round {CurrentRoundIndex + 1}";
}
