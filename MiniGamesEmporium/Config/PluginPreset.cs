using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using System;

/// <summary>A named snapshot of game rules, chat messages, and queue settings used to switch between venue configurations.</summary>

namespace MiniGamesEmporium.Config;
[Serializable]
public sealed class PluginPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New Preset";
    public string QueueKeyword { get; set; } = "!join";
    public QueueJoinChannelsConfig QueueJoinChannels { get; set; } = new();
    public Bar777Config Bar777 { get; set; } = new();
    public DeathrollTournamentConfig DeathrollTournament { get; set; } = new();
}
