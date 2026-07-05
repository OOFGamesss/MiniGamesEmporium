using System;
using System.Collections.Generic;

/// <summary>Serialisable snapshot of one player's active Higher/Lower turn.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Models;
[Serializable]
public class HigherLowerTurnState
{
    public List<int> RollLog { get; set; } = [];
    public int RoundsCorrect { get; set; } = 0;
    public int? DetectedGuess { get; set; } = null;
    public bool GuessConfirmed { get; set; } = false;
    public bool IsGameOver { get; set; } = false;
    public bool IsWinner { get; set; } = false;
    public long WinnerPayoutGil { get; set; } = 0;
}
