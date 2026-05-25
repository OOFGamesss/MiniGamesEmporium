using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using System;
using System.Collections.Generic;

/// <summary>Serialisable payload for exporting and importing preset sections as a Base64-encoded JSON string. Only present sections are applied on import; absent sections fall back to defaults.</summary>

namespace MiniGamesEmporium.Config;

[Serializable]
public sealed class PresetExportPayload
{
    public int Version { get; set; } = 1;
    public Bar777ExportEntry? Bar777 { get; set; }
    public DeathrollExportEntry? DeathrollTournament { get; set; }
    public DiscordExportEntry? Discord { get; set; }
    public string? QueueKeyword { get; set; }
    public QueueJoinChannelsConfig? QueueJoinChannels { get; set; }
}

[Serializable]
public sealed class Bar777ExportEntry
{
    public string CustomName { get; set; } = "BAR 777";
    public int CostPerRoll { get; set; } = 100_000;
    public int MaxRolls { get; set; } = 20;
    public int WinNumber { get; set; } = 777;
    public bool UseQueue { get; set; } = false;
    public bool AutoCatchRoll { get; set; } = false;
    public Bar777ChatConfig Chat { get; set; } = new();
}

[Serializable]
public sealed class DeathrollExportEntry
{
    public long EntryCost { get; set; } = 100_000;
    public List<int> BestOfPerRound { get; set; } = new() { 1, 3, 5, 7, 9 };
    public bool AutoNextMatch { get; set; } = false;
    public int AutoNextMatchDelaySeconds { get; set; } = 5;
    public bool AutoCatchNextRound { get; set; } = false;
    public DeathrollTournamentChatConfig Chat { get; set; } = new();
}

[Serializable]
public sealed class DiscordExportEntry
{
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}
