using System;
using System.Collections.Generic;
using MiniGamesEmporium.Games.CoinCollector.Models;

/// <summary>Serialisable configuration for the Coin Collector game.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Config;
[Serializable]
public class CoinCollectorConfig
{
    public int EntryCost { get; set; } = 100_000;
    public long BoostedPot { get; set; } = 0L;
    public int StartingRollMax { get; set; } = 999;
    public bool AutoWinCount { get; set; } = true;
    public int TargetCoins { get; set; } = 5;
    public bool AllowMultipleWinners { get; set; } = true;
    public int TradesToPotPercent { get; set; } = 100;
    public long SessionTradedTotal { get; set; } = 0L;
    public int PlayersPlayed { get; set; } = 0;
    public List<CoinCollectorLeaderboardEntry> SessionLeaderboard { get; set; } = [];
    public bool SessionFinished { get; set; } = false;
    public List<CoinCollectorWinnerPayout> WinnerPayouts { get; set; } = [];
    public List<CoinCollectorQueueEntry> PlayerQueue { get; set; } = [];
    public CoinCollectorAttemptState Attempts { get; set; } = new();
    public CoinCollectorChatConfig Chat { get; set; } = new();
    public bool TradeOnRequestGil { get; set; } = false;
    public bool AutoBeginOnPayment { get; set; } = false;
    public bool AutoEndTurn { get; set; } = false;
    public int AutoEndTurnDelayMs { get; set; } = 3000;
    public bool AllowMultipleAttempts { get; set; } = true;
}
