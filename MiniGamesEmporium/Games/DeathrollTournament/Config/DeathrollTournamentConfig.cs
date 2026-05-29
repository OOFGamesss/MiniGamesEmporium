using System;
using System.Collections.Generic;

/// <summary>Serialisable configuration for the Deathroll Tournament game, storing entry cost, boosted pot, per-round best-of settings, the pre-tournament player registration list, and the nested chat configuration.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Config;
[Serializable]
public class DeathrollTournamentConfig
{
    public long EntryCost { get; set; } = 100_000;
    public long BoostedPot { get; set; } = 0L;
    public List<int> BestOfPerRound { get; set; } = new() { 1, 3, 5, 7, 9 };
    public List<string> RegisteredPlayers { get; set; } = new();
    public List<string> PaidPlayers { get; set; } = new();
    public List<string> UnverifiedPlayers { get; set; } = new();
    public Dictionary<string, string> PlayerBuyers { get; set; } = new();
    public bool AutoNextMatch { get; set; } = false;
    public int AutoNextMatchDelaySeconds { get; set; } = 5;
    public bool AutoCatchNextRound { get; set; } = false;
    public DeathrollTournamentChatConfig Chat { get; set; } = new();
    public DeathrollTournamentDiscordEntry Discord { get; set; } = new();
}
