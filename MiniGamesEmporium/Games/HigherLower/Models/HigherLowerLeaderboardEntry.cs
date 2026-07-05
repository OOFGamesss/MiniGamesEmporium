using System;

/// <summary>Represents one completed turn in the Higher/Lower leaderboard.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Models;
[Serializable]
public class HigherLowerLeaderboardEntry
{
    public string PlayerName { get; set; } = string.Empty;
    public int RoundsCorrect { get; set; } = 0;
    public bool IsWinner { get; set; } = false;
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
