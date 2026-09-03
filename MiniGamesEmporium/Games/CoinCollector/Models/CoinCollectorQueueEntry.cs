using System;

/// <summary>One player on the Coin Collector join-order roster, kept stable for the whole session.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Models;
[Serializable]
public class CoinCollectorQueueEntry
{
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerWorld { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public int TurnsTaken { get; set; } = 0;
    public int BestCoins { get; set; } = 0;

    public string DisplayName =>
        string.IsNullOrEmpty(this.PlayerWorld) ? this.PlayerName : $"{this.PlayerName}@{this.PlayerWorld}";
}
