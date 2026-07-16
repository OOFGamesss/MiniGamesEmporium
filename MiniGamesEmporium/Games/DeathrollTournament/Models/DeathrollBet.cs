using System;

/// <summary>A single declared bet on which entrant will win the tournament.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Models;
[Serializable]
public class DeathrollBet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BettorName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = false;
    public bool NeedsReview { get; set; } = false;
    public bool FromChat { get; set; } = false;
    public DateTime DeclaredAt { get; set; } = DateTime.UtcNow;
}
