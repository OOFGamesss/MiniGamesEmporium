using System;
using System.Collections.Generic;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.HigherLower.Config;

/// <summary>Serialisable payload for exporting and importing presets as Base64 JSON.</summary>

namespace MiniGamesEmporium.Models;

[Serializable]
public sealed class PresetExportPayload
{
    public int Version { get; set; } = 1;
    public Bar777ExportEntry? Bar777 { get; set; }
    public DeathrollExportEntry? DeathrollTournament { get; set; }
    public HigherLowerExportEntry? HigherLower { get; set; }
    public DiscordExportEntry? Discord { get; set; }
    public string? QueueKeyword { get; set; }
    public QueueConfig? QueueJoinChannels { get; set; }
}

[Serializable]
public sealed class HigherLowerExportEntry
{
    public int EntryCost { get; set; } = 100_000;
    public int DiceSides { get; set; } = 10;
    public bool AutoWinCount { get; set; } = true;
    public int TargetRounds { get; set; } = 5;
    public bool AllowMultipleWinners { get; set; } = true;
    public int TradesToPotPercent { get; set; } = 100;
    public HigherLowerChatConfig Chat { get; set; } = new();
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
    public bool AutoJoinKeyword { get; set; } = false;
    public string JoinKeyword { get; set; } = "!join";
    public QueueConfig JoinChannels { get; set; } = new();
    public DeathrollTournamentChatConfig Chat { get; set; } = new();
}

[Serializable]
public sealed class DiscordExportEntry
{
    public List<DiscordWebhookExportItem> Webhooks { get; set; } = new();
    public string WebhookUsername { get; set; } = "Deathroll Tournament";
    public string WebhookAvatarUrl { get; set; } = "https://raw.githubusercontent.com/OOFGamesss/OOFGamesPlugins/main/images/deathrolltournament.png";
}

[Serializable]
public sealed class DiscordWebhookExportItem
{
    public string Alias { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}
