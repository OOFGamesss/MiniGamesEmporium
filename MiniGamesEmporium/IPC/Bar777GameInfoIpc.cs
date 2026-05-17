using System;
using System.Linq;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777;

namespace MiniGamesEmporium.IPC;

[Serializable]
public sealed class Bar777GameInfoIpc
{
    public string GameLabel { get; set; } = string.Empty;
    public long BoostedPot { get; set; }
    public long TotalPot { get; set; }
    public long CostPerRoll { get; set; }
    public int MaxRolls { get; set; }
    public int PlayersPlayed { get; set; }
    public int? Queue { get; set; }

    internal static Bar777GameInfoIpc? FromConfig(PluginConfiguration config)
    {
        if (config.ActiveSession == null) return null;

        var amountInTrades = config.Transactions
            .Where(t => t.GameName == Bar777GameIds.DisplayName)
            .Sum(t => (long)t.Amount);

        return new Bar777GameInfoIpc
        {
            BoostedPot    = config.Bar777.BoostedPot,
            TotalPot      = config.Bar777.BoostedPot + amountInTrades,
            CostPerRoll   = config.Bar777.CostPerRoll,
            MaxRolls      = config.Bar777.MaxRolls,
            PlayersPlayed = config.Bar777.PlayersPlayed,
            Queue         = config.Bar777.UseQueue ? config.QueuedPlayers.Count : null,
            GameLabel     = $"BAR {config.Bar777.WinNumber}",
        };
    }
}
