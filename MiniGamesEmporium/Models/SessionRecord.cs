using System;

/// <summary>Serialisable record of a completed game session.</summary>

namespace MiniGamesEmporium.Models;
[Serializable]
public class SessionRecord
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public string GameName { get; set; } = string.Empty;
    public string Winner { get; set; } = string.Empty;
    public long BoostedPot { get; set; }
    public long AmountInTrades { get; set; }
    public long KeptFromTrades { get; set; }
    public long TotalPot { get; set; }
    public int PlayersPlayed { get; set; }
    public int? RoundsPlayed { get; set; }
    public int? MatchesPlayed { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
