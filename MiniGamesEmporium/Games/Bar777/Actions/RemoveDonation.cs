using MiniGamesEmporium.Config;
using MiniGamesEmporium.State;
using System;

/// <summary>Subtracts a manual withdrawal from the BAR 777 boosted pot and records it as a negative transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class RemoveDonation
{
    public static void Execute(PluginConfiguration config, int amount)
    {
        if (amount <= 0) return;
        var actualRemoved = (int)Math.Min(amount, config.Bar777.BoostedPot);
        if (actualRemoved <= 0) return;
        config.Bar777.BoostedPot -= actualRemoved;
        config.Transactions.Add(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = -actualRemoved,
            Timestamp  = DateTime.UtcNow,
            GameName   = config.Bar777.CustomName,
        });
        config.Save();
    }
}
