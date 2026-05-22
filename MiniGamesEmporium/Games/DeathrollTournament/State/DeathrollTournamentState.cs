using System;
using System.Collections.Generic;

/// <summary>Defines the phase of an active deathroll match within the bracket.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.State;
public enum MatchPhase
{
    NotStarted,
    DeterminingOrder,
    Deathrolling,
    GameOver,
    MatchComplete,
}

/// <summary>Serialisable snapshot of a live Deathroll Tournament session, holding the full bracket structure, current match progress, active roll log, and the overall tournament winner once resolved.</summary>
[Serializable]
public class DeathrollTournamentState
{
    public string GameName { get; set; } = "Deathroll Tournament";
    public long EntryCostAtStart { get; set; } = 0;
    public long BoostedPotAtStart { get; set; } = 0;
    public int PlayerCountAtStart { get; set; } = 0;
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
}
