using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.State;
using System;

/// <summary>Adds a manual donation to the Deathroll Tournament pot and records it as a transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AddDonation
{
    public static void Execute(PluginConfiguration config, long amount)
    {
        if (amount <= 0) return;
        var tournament = config.DeathrollTournamentSession;
        if (tournament != null)
            tournament.PotAdjustment += amount;
        else
        {
            var activeSession = config.DeathrollSession;
            if (activeSession == null) return;
            activeSession.PotAdjustment += amount;
        }
        config.Transactions.Add(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = (int)amount,
            Timestamp  = DateTime.UtcNow,
            GameName   = DeathrollGameIds.DisplayName,
        });
        config.Save();
    }
}
