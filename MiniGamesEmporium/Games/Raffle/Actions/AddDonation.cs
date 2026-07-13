using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;

/// <summary>Adds a manual donation to the Raffle pot and records it as a transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class AddDonation
{
    public static void Execute(PluginConfiguration config, HistoryService historyService, long amount)
    {
        if (amount <= 0) return;
        var state = config.RaffleSession;
        if (state == null) return;
        state.PotAdjustment += amount;
        historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = (int)amount,
            Timestamp  = DateTime.UtcNow,
            GameName   = RaffleGameIds.DisplayName,
        });
        config.Save();
    }
}
