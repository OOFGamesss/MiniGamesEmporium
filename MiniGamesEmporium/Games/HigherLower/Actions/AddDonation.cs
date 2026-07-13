using System;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;

/// <summary>Adds a manual donation to the Higher/Lower pot and records it as a transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class AddDonation
{
    public static void Execute(PluginConfiguration config, HistoryService historyService, int amount)
    {
        if (amount <= 0) return;
        config.HigherLower.BoostedPot += amount;
        historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = amount,
            Timestamp  = DateTime.UtcNow,
            GameName   = HigherLowerGameIds.DisplayName,
        });
        config.Save();
    }
}
