using System;

/// <summary>Tracks how many paid roll attempts the current Coin Collector player has and how many are used.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Models;
[Serializable]
public class CoinCollectorAttemptState
{
    public string PlayerName { get; set; } = string.Empty;
    public int AttemptsPurchased { get; set; } = 0;
    public int AttemptsUsed { get; set; } = 0;
    public bool PaymentAnnounced { get; set; } = false;

    public int Remaining => Math.Max(0, this.AttemptsPurchased - this.AttemptsUsed);

    public void Reset()
    {
        this.PlayerName        = string.Empty;
        this.AttemptsPurchased = 0;
        this.AttemptsUsed      = 0;
        this.PaymentAnnounced  = false;
    }
}
