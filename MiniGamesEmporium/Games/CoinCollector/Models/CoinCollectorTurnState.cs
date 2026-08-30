using System;
using System.Collections.Generic;

/// <summary>Serialisable snapshot of one player's active Coin Collector turn.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Models;
[Serializable]
public class CoinCollectorTurnState
{
    public List<int> RollLog { get; set; } = [];
    public List<int> RollMaxLog { get; set; } = [];
    public int CoinsCollected { get; set; } = 0;
    public int CurrentRollMax { get; set; } = 0;
    public bool IsGameOver { get; set; } = false;
    public bool IsWinner { get; set; } = false;
    public long WinnerPayoutGil { get; set; } = 0;
}
