using System;

/// <summary>Represents one match in the tournament bracket, storing both player slots, the winner once resolved, per-player game win counts for best-of series, and the resolved flag.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.State;
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
