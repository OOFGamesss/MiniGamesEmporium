using System;

/// <summary>Represents one completed turn in the Coin Collector leaderboard.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Models;
[Serializable]
public class CoinCollectorLeaderboardEntry
{
    public string PlayerName { get; set; } = string.Empty;
    public int Coins { get; set; } = 0;
    public bool IsWinner { get; set; } = false;
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}
