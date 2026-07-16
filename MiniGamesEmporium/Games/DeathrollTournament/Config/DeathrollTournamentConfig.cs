using System;
using System.Collections.Generic;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;

/// <summary>Serialisable configuration for the Deathroll Tournament game.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Config;
[Serializable]
public class DeathrollTournamentConfig
{
    public long EntryCost { get; set; } = 100_000;
    public long BoostedPot { get; set; } = 0L;
    public DeathrollPrizeType PrizeType { get; set; } = DeathrollPrizeType.Gil;
    public uint PrizeItemId { get; set; } = 0;
    public string PrizeItemName { get; set; } = string.Empty;
    public string PrizeCustomText { get; set; } = string.Empty;
    public bool AutoJoinKeyword { get; set; } = false;
    public string JoinKeyword { get; set; } = "!join";
    public QueueConfig JoinChannels { get; set; } = new();
    public bool BettingEnabled { get; set; } = false;
    public long BetUnit { get; set; } = 50_000;
    public bool AutoBetKeyword { get; set; } = false;
    public string BetKeyword { get; set; } = "!bet";
    public QueueConfig BetChannels { get; set; } = new();
    public List<DeathrollBet> Bets { get; set; } = new();
    public Dictionary<string, long> BettorCredit { get; set; } = new();
    public List<int> BestOfPerRound { get; set; } = new() { 1, 3, 5, 7, 9 };
    public List<string> RegisteredPlayers { get; set; } = new();
    public List<string> PaidPlayers { get; set; } = new();
    public List<string> UnverifiedPlayers { get; set; } = new();
    public Dictionary<string, string> PlayerBuyers { get; set; } = new();
    public bool AutoNextMatch { get; set; } = false;
    public int AutoNextMatchDelaySeconds { get; set; } = 5;
    public bool AutoCatchNextRound { get; set; } = false;
    public string WebhookUsername { get; set; } = "Deathroll Tournament";
    public string WebhookAvatarUrl { get; set; } = "https://raw.githubusercontent.com/OOFGamesss/OOFGamesPlugins/main/images/deathrolltournament.png";
    public DeathrollTournamentChatConfig Chat { get; set; } = new();
    public List<DeathrollTournamentDiscordEntry> DiscordWebhooks { get; set; } = new();
    public bool WebMirrorEnabled { get; set; } = false;
    public string WebSessionId { get; set; } = string.Empty;
    public string WebSpectatorUrl { get; set; } = string.Empty;
    public string WebVenueName { get; set; } = string.Empty;
    public string WebVenueImageUrl { get; set; } = string.Empty;
}
