using Dalamud.Configuration;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.Bar777.State;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.State;
using System;
using System.Collections.Generic;

/// <summary>Root plugin configuration, serialised by Dalamud, holding all game settings, queue state, session history, and transaction records.</summary>

namespace MiniGamesEmporium.Config;
[Serializable]
public class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 2;
    public Bar777Config Bar777 { get; set; } = new();
    public DeathrollTournamentConfig DeathrollTournament { get; set; } = new();
    public DeathrollSessionInfo? DeathrollSession { get; set; } = null;
    public DeathrollTournamentState? DeathrollTournamentSession { get; set; } = null;
    public string QueueKeyword { get; set; } = "!join";
    public QueueJoinChannelsConfig QueueJoinChannels { get; set; } = new();
    public List<TransactionRecord> Transactions { get; set; } = new();
    public List<SessionRecord> SessionHistory { get; set; } = new();
    public ActiveSessionState? ActiveSession { get; set; } = null;
    public List<string> QueuedPlayers { get; set; } = new();
    public List<PluginPreset> Presets { get; set; } = new();
    public int ActivePresetIndex { get; set; } = -1;
    public void Save()
    {
        MiniGamesEmporium.PluginInterface.SavePluginConfig(this);
    }
}
