using System;

/// <summary>Serialisable record of a single Gil trade transaction.</summary>

namespace MiniGamesEmporium.Models;
[Serializable]
public class TransactionRecord
{
    public Guid TransactionId { get; set; } = Guid.NewGuid();
    public string PlayerName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string GameName { get; set; } = string.Empty;
}
