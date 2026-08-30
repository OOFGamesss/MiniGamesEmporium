using System;

/// <summary>Per-winner payout record for a finished Coin Collector session.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Models;
[Serializable]
public class CoinCollectorWinnerPayout
{
    public string PlayerName { get; set; } = string.Empty;
    public long PaidGil { get; set; } = 0L;
    public Guid? PayoutTransactionId { get; set; } = null;
}
