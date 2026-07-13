using System;

/// <summary>Serialisable configuration for the BAR 777 game.</summary>

namespace MiniGamesEmporium.Games.Bar777.Config;
[Serializable]
public class Bar777Config
{
    public string CustomName { get; set; } = "BAR 777";
    public int CostPerRoll { get; set; } = 100_000;
    public int MaxRolls { get; set; } = 20;
    public int WinNumber { get; set; } = 777;
    public long BoostedPot { get; set; } = 0L;
    public long SessionTradedTotal { get; set; } = 0L;
    public bool UseQueue { get; set; } = false;
    public int TradesToPotPercent { get; set; } = 100;
    public bool AutoCatchRoll { get; set; } = false;
    public int PlayersPlayed { get; set; } = 0;
    public Bar777ChatConfig Chat { get; set; } = new();
}
