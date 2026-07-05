using System;
using System.Collections.Generic;

using MiniGamesEmporium.Games.HigherLower.Models;

/// <summary>Serialisable configuration for the Higher/Lower game.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Config;
[Serializable]
public class HigherLowerConfig
{
    public int EntryCost { get; set; } = 100_000;
    public long BoostedPot { get; set; } = 0L;
    public int DiceSides { get; set; } = 10;
    public bool AutoWinCount { get; set; } = true;
    public int TargetRounds { get; set; } = 5;
    public bool AllowMultipleWinners { get; set; } = true;
    public int TradesToPotPercent { get; set; } = 100;
    public long SessionTradedTotal { get; set; } = 0L;
    public int PlayersPlayed { get; set; } = 0;
    public List<HigherLowerLeaderboardEntry> SessionLeaderboard { get; set; } = [];
    public bool SessionFinished { get; set; } = false;
    public List<HigherLowerWinnerPayout> WinnerPayouts { get; set; } = [];
    public HigherLowerChatConfig Chat { get; set; } = new();
    public long ComputeTotalPot() => BoostedPot + (SessionTradedTotal * TradesToPotPercent / 100);
    public long ComputeTradesHeldBack() => SessionTradedTotal - (SessionTradedTotal * TradesToPotPercent / 100);
}
