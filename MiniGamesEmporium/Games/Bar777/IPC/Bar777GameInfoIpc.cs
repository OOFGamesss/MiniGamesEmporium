using MiniGamesEmporium.Config;
using System;

/// <summary>Serialisable snapshot of the current BAR 777 session state, exposed via the IPC gate for consumption by external plugins.</summary>

namespace MiniGamesEmporium.Games.Bar777.IPC;

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

        return new Bar777GameInfoIpc
        {
            BoostedPot    = config.Bar777.BoostedPot,
            TotalPot      = config.Bar777.BoostedPot + config.Bar777.SessionTradedTotal,
            CostPerRoll   = config.Bar777.CostPerRoll,
            MaxRolls      = config.Bar777.MaxRolls,
            PlayersPlayed = config.Bar777.PlayersPlayed,
            Queue         = config.Bar777.UseQueue ? config.QueuedPlayers.Count : null,
            GameLabel     = config.Bar777.CustomName,
        };
    }
}
