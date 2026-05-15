using Dalamud.Configuration;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.Bar777.State;
using MiniGamesEmporium.State;
using System;
using System.Collections.Generic;

/// <summary>Root plugin configuration, serialised by Dalamud, holding all Bar777 settings, queue state, session history, and transaction records.</summary>

namespace MiniGamesEmporium.Config;
[Serializable]
public class PluginConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 2;
    public Bar777Config Bar777 { get; set; } = new();
    public string QueueKeyword { get; set; } = "!join";
    public QueueJoinChannelsConfig QueueJoinChannels { get; set; } = new();
    public List<TransactionRecord> Transactions { get; set; } = new();
    public List<SessionRecord> SessionHistory { get; set; } = new();
    public ActiveSessionState? ActiveSession { get; set; } = null;
    public List<string> QueuedPlayers { get; set; } = new();
    public void Save()
    {
        MiniGamesEmporium.PluginInterface.SavePluginConfig(this);
    }
}
