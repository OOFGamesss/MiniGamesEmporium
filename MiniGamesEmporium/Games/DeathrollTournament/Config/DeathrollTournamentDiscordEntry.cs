using System;

/// <summary>Persisted state for one Deathroll Tournament Discord webhook.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Config;
[Serializable]
public class DeathrollTournamentDiscordEntry
{
    public string Alias { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? MessageId { get; set; } = null;
    public bool Enabled { get; set; } = false;
    public bool PostFailed { get; set; } = false;
}
