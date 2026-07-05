using System;

/// <summary>Represents one match in the tournament bracket.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Models;
[Serializable]
public class BracketMatch
{
    public string Player1 { get; set; } = string.Empty;
    public string Player2 { get; set; } = string.Empty;
    public string Winner { get; set; } = string.Empty;
    public int Player1Wins { get; set; } = 0;
    public int Player2Wins { get; set; } = 0;
    public bool IsResolved { get; set; } = false;
}
