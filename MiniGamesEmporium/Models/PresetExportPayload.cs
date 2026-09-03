using System;
using System.Collections.Generic;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.CoinCollector.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.HigherLower.Config;
using MiniGamesEmporium.Games.Raffle.Config;
using MiniGamesEmporium.Games.VotingMadness.Config;

/// <summary>Serialisable payload for exporting and importing presets as Base64 JSON.</summary>

namespace MiniGamesEmporium.Models;

[Serializable]
public sealed class PresetExportPayload
{
    public int Version { get; set; } = 1;
    public Bar777ExportEntry? Bar777 { get; set; }
    public DeathrollExportEntry? DeathrollTournament { get; set; }
    public RaffleExportEntry? Raffle { get; set; }
    public HigherLowerExportEntry? HigherLower { get; set; }
    public VotingMadnessExportEntry? VotingMadness { get; set; }
    public CoinCollectorExportEntry? CoinCollector { get; set; }
    public DiscordExportEntry? Discord { get; set; }
    public string? QueueKeyword { get; set; }
    public QueueConfig? QueueJoinChannels { get; set; }
}

[Serializable]
public sealed class Bar777ExportEntry
{
    public string CustomName { get; set; } = "BAR 777";
    public int CostPerRoll { get; set; } = 100_000;
    public int MaxRolls { get; set; } = 20;
    public int WinNumber { get; set; } = 777;
    public long BoostedPot { get; set; } = 0L;
    public int TradesToPotPercent { get; set; } = 100;
    public bool UseQueue { get; set; } = false;
    public bool AutoCatchRoll { get; set; } = false;
    public Bar777ChatConfig Chat { get; set; } = new();
}

[Serializable]
public sealed class DeathrollExportEntry
{
    public long EntryCost { get; set; } = 100_000;
    public long BoostedPot { get; set; } = 0L;
    public DeathrollPrizeType PrizeType { get; set; } = DeathrollPrizeType.Gil;
    public uint PrizeItemId { get; set; } = 0;
    public string PrizeItemName { get; set; } = string.Empty;
    public string PrizeCustomText { get; set; } = string.Empty;
    public List<int> BestOfPerRound { get; set; } = new() { 1, 3, 5, 7, 9 };
    public bool AutoNextMatch { get; set; } = false;
    public int AutoNextMatchDelaySeconds { get; set; } = 5;
    public bool AutoCatchNextRound { get; set; } = false;
    public bool AutoJoinKeyword { get; set; } = false;
    public string JoinKeyword { get; set; } = "!join";
    public QueueConfig JoinChannels { get; set; } = new();
    public bool BettingEnabled { get; set; } = false;
    public long BetUnit { get; set; } = 50_000;
    public bool AutoBetKeyword { get; set; } = false;
    public string BetKeyword { get; set; } = "!bet";
    public QueueConfig BetChannels { get; set; } = new();
    public string WebVenueName { get; set; } = string.Empty;
    public string WebVenueImageUrl { get; set; } = string.Empty;
    public DeathrollTournamentChatConfig Chat { get; set; } = new();
}

[Serializable]
public sealed class RaffleExportEntry
{
    public long TicketCost { get; set; } = 100_000;
    public int MaxTicketsPerPlayer { get; set; } = 999;
    public long BoostedPot { get; set; } = 0L;
    public int TradesToPotPercent { get; set; } = 100;
    public bool ShuffleTicketNumbers { get; set; } = false;
    public int CloseHour { get; set; } = -1;
    public int CloseMinute { get; set; } = 0;
    public bool AutoJoinKeyword { get; set; } = false;
    public string JoinKeyword { get; set; } = "!join";
    public QueueConfig JoinChannels { get; set; } = new();
    public RaffleChatConfig Chat { get; set; } = new();
}

[Serializable]
public sealed class HigherLowerExportEntry
{
    public int EntryCost { get; set; } = 100_000;
    public long BoostedPot { get; set; } = 0L;
    public int DiceSides { get; set; } = 10;
    public bool AutoWinCount { get; set; } = true;
    public int TargetRounds { get; set; } = 5;
    public bool AllowMultipleWinners { get; set; } = true;
    public int TradesToPotPercent { get; set; } = 100;
    public HigherLowerChatConfig Chat { get; set; } = new();
}

[Serializable]
public sealed class VotingMadnessExportEntry
{
    public List<string> Options { get; set; } = new() { "Yes", "No" };
    public QueueConfig VoteChannels { get; set; } = new();
    public bool MultipleChoice { get; set; } = false;
    public bool AllowMultipleVotes { get; set; } = false;
    public int CloseHour { get; set; } = -1;
    public int CloseMinute { get; set; } = 0;
    public VotingMadnessChatConfig Chat { get; set; } = new();
}

[Serializable]
public sealed class CoinCollectorExportEntry
{
    public int EntryCost { get; set; } = 100_000;
    public long BoostedPot { get; set; } = 0L;
    public int StartingRollMax { get; set; } = 999;
    public bool AutoWinCount { get; set; } = true;
    public int TargetCoins { get; set; } = 5;
    public bool AllowMultipleWinners { get; set; } = true;
    public int TradesToPotPercent { get; set; } = 100;
    public CoinCollectorChatConfig Chat { get; set; } = new();
    public bool TradeOnRequestGil { get; set; } = false;
    public bool AutoBeginOnPayment { get; set; } = false;
    public bool AutoEndTurn { get; set; } = false;
    public int AutoEndTurnDelayMs { get; set; } = 3000;
    public bool AllowMultipleAttempts { get; set; } = true;
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
