using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;

/// <summary>Adds a manual donation to the Coin Collector pot and records it as a transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class AddDonation
{
    public static void Execute(PluginConfiguration config, HistoryService historyService, int amount)
    {
        if (amount <= 0) return;
        config.CoinCollector.BoostedPot += amount;
        historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = amount,
            Timestamp  = DateTime.UtcNow,
            GameName   = CoinCollectorGameIds.DisplayName,
        });
        config.Save();
    }
}
