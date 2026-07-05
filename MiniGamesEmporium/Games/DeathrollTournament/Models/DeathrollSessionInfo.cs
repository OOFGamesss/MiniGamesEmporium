using System;

/// <summary>Entry cost and boosted pot locked in when a Deathroll Tournament session opens.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Models;
[Serializable]
public class DeathrollSessionInfo
{
    public long EntryCost { get; set; }
    public long BoostedPot { get; set; }
    public long PotAdjustment { get; set; } = 0L;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
