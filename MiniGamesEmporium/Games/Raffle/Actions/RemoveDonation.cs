using System;
using System.Linq;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;

/// <summary>Subtracts a manual withdrawal from the Raffle pot and records it as a negative transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class RemoveDonation
{
    public static void Execute(PluginConfiguration config, HistoryService historyService, long amount)
    {
        if (amount <= 0) return;
        var state = config.RaffleSession;
        if (state == null) return;
        var ticketsSold = state.Blocks.Sum(b => b.Count);
        var tradesToPot = state.TicketCostAtStart * ticketsSold * state.TradesToPotPercentAtStart / 100;
        var currentPot  = tradesToPot + state.BoostedPotAtStart + state.PotAdjustment;
        var actualRemoved = Math.Min(amount, Math.Max(0, currentPot));
        if (actualRemoved <= 0) return;
        state.PotAdjustment -= actualRemoved;
        historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = (int)-actualRemoved,
            Timestamp  = DateTime.UtcNow,
            GameName   = RaffleGameIds.DisplayName,
        });
        config.Save();
    }
}
