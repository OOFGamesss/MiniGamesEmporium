using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;

/// <summary>Subtracts a manual withdrawal from the Coin Collector boosted pot and records it as a negative transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class RemoveDonation
{
    public static void Execute(PluginConfiguration config, HistoryService historyService, int amount)
    {
        if (amount <= 0) return;
        var actualRemoved = (int)Math.Min((long)amount, config.CoinCollector.BoostedPot);
        if (actualRemoved <= 0) return;
        config.CoinCollector.BoostedPot -= actualRemoved;
        historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = -actualRemoved,
            Timestamp  = DateTime.UtcNow,
            GameName   = CoinCollectorGameIds.DisplayName,
        });
        config.Save();
    }
}
