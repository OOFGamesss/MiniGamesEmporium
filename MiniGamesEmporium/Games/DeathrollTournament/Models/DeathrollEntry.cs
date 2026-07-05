using System;

/// <summary>Records a single deathroll within an active match.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Models;
[Serializable]
public class DeathrollEntry
{
    public string PlayerName { get; set; } = string.Empty;
    public int RollMax { get; set; } = 0;
    public int RollValue { get; set; } = 0;
}
