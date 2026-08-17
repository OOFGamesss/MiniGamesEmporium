using System;
using MiniGamesEmporium.Config;

/// <summary>Serialisable configuration and pre-session defaults for the Raffle game.</summary>

namespace MiniGamesEmporium.Games.Raffle.Config;
[Serializable]
public class RaffleConfig
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
