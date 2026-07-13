using System;

/// <summary>Per-winner payout record for a finished Higher/Lower session.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Models;
[Serializable]
public class HigherLowerWinnerPayout
{
    public string PlayerName { get; set; } = string.Empty;
    public long PaidGil { get; set; } = 0L;
    public Guid? PayoutTransactionId { get; set; } = null;
}
