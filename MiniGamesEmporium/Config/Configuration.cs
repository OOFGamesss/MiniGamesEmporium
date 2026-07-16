using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.HigherLower.Config;
using MiniGamesEmporium.Games.HigherLower.Models;
using MiniGamesEmporium.Games.Raffle.Config;
using MiniGamesEmporium.Games.Raffle.State;
using MiniGamesEmporium.Models;

/// <summary>Root plugin configuration serialised by Dalamud.</summary>

namespace MiniGamesEmporium.Config;
[Serializable]
public class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public Bar777Config Bar777 { get; set; } = new();
    public WebviewConfig Webview { get; set; } = new();
    public DeathrollTournamentConfig DeathrollTournament { get; set; } = new();
    public HigherLowerConfig HigherLower { get; set; } = new();
    public RaffleConfig Raffle { get; set; } = new();
    public string QueueKeyword { get; set; } = "!join";
    public QueueConfig QueueJoinChannels { get; set; } = new();
    public List<PluginPreset> Presets { get; set; } = new();
    public int ActivePresetIndex { get; set; } = -1;

    public HigherLowerTurnState? HigherLowerSession { get; set; } = null;
    public DeathrollSessionInfo? DeathrollSession { get; set; } = null;
    public DeathrollTournamentState? DeathrollTournamentSession { get; set; } = null;
    public RaffleState? RaffleSession { get; set; } = null;
    public ActiveSession? ActiveSession { get; set; } = null;
    public List<string> QueuedPlayers { get; set; } = new();

    public bool HasMigratedHistoryToLiteDb { get; set; } = false;
    public List<TransactionRecord> Transactions { get; set; } = new();
    public List<SessionRecord> SessionHistory { get; set; } = new();
    public void Save()
    {
        MiniGamesEmporium.PluginInterface.SavePluginConfig(this);
    }
}
