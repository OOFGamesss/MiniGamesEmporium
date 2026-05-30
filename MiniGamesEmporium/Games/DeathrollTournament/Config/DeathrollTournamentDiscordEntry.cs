using System;

/// <summary>Persisted state for the single Deathroll Tournament Discord webhook: URL, the Discord message ID of the managed embed, delivery status flags, and appearance overrides.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Config;
[Serializable]
public class DeathrollTournamentDiscordEntry
{
    public string Url { get; set; } = string.Empty;
    public string? MessageId { get; set; } = null;
    public bool Enabled { get; set; } = false;
    public bool PostFailed { get; set; } = false;
    public string WebhookUsername { get; set; } = "Deathroll Tournament";
    public string WebhookAvatarUrl { get; set; } = "https://raw.githubusercontent.com/OOFGamesss/OOFGamesPlugins/main/images/deathrolltournament.png";
}
