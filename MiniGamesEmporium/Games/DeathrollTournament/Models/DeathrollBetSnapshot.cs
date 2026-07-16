using System;

/// <summary>A bettor and target pair locked in when the tournament starts.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Models;
[Serializable]
public class DeathrollBetSnapshot
{
    public string BettorName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
}
