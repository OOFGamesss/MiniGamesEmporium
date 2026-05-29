using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.State;
using System;

/// <summary>Subtracts a manual withdrawal from the Deathroll Tournament pot and records it as a negative transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class RemoveDonation
{
    public static void Execute(PluginConfiguration config, HistoryService historyService, long amount)
    {
        if (amount <= 0) return;
        long actualRemoved;
        var tournament = config.DeathrollTournamentSession;
        if (tournament != null)
        {
            var currentPot = tournament.EntryCostAtStart * tournament.PlayerCountAtStart + tournament.BoostedPotAtStart + tournament.PotAdjustment;
            actualRemoved = Math.Min(amount, Math.Max(0, currentPot));
            tournament.PotAdjustment -= actualRemoved;
        }
        else
        {
            var activeSession = config.DeathrollSession;
            if (activeSession == null) return;
            var cfg        = config.DeathrollTournament;
            var currentPot = activeSession.EntryCost * cfg.PaidPlayers.Count + activeSession.BoostedPot + activeSession.PotAdjustment;
            actualRemoved = Math.Min(amount, Math.Max(0, currentPot));
            activeSession.PotAdjustment -= actualRemoved;
        }
        if (actualRemoved <= 0) return;
        historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = (int)-actualRemoved,
            Timestamp  = DateTime.UtcNow,
            GameName   = DeathrollGameIds.DisplayName,
        });
        config.Save();
    }
}
