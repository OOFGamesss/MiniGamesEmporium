using System;

/// <summary>Records a single deathroll within an active match, capturing the rolling player's name, the maximum they rolled against, and the value they rolled.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.State;
[Serializable]
public class DeathrollEntry
{
    public string PlayerName { get; set; } = string.Empty;
    public int RollMax { get; set; } = 0;
    public int RollValue { get; set; } = 0;
}
