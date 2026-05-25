using System;

/// <summary>Lightweight record capturing the entry cost and boosted pot locked in when a Deathroll Tournament session is opened from the start door.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.State;
[Serializable]
public class DeathrollSessionInfo
{
    public long EntryCost { get; set; }
    public long BoostedPot { get; set; }
    public long PotAdjustment { get; set; } = 0L;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
