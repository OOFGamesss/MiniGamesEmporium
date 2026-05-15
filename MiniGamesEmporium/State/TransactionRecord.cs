using System;

/// <summary>Serialisable record of a single Gil trade transaction, storing the player name, Gil amount, associated game name, and the time the trade was completed.</summary>

namespace MiniGamesEmporium.State;
[Serializable]
public class TransactionRecord
{
    public string PlayerName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string GameName { get; set; } = "BAR 777";
}
