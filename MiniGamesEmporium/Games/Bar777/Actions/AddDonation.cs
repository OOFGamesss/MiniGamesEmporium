using MiniGamesEmporium.Config;
using MiniGamesEmporium.State;
using System;

/// <summary>Adds a manual donation to the BAR 777 pot and records it as a transaction for audit purposes.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AddDonation
{
    public static void Execute(PluginConfiguration config, int amount)
    {
        if (amount <= 0) return;
        config.Bar777.BoostedPot += amount;
        config.Transactions.Add(new TransactionRecord
        {
            PlayerName = "Manual Pot Adjustment",
            Amount     = amount,
            Timestamp  = DateTime.UtcNow,
            GameName   = config.Bar777.CustomName,
        });
        config.Save();
    }
}
