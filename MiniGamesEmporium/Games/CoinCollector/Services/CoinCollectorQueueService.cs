using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.DalamudServices;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Models;
using MiniGamesEmporium.Services;

/// <summary>Keeps a stable join-order roster of Coin Collector players, independent of party list order.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Services;
public sealed class CoinCollectorQueueService : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly CoinCollectorService coinCollectorService;
    private readonly PlayerInfoService playerInfo;

    public CoinCollectorQueueService(PluginConfiguration config, CoinCollectorService coinCollectorService, PlayerInfoService playerInfo)
    {
        this.config               = config;
        this.coinCollectorService = coinCollectorService;
        this.playerInfo           = playerInfo;
        this.coinCollectorService.TurnCompleted += OnTurnCompleted;
    }

    public IReadOnlyList<CoinCollectorQueueEntry> GetRoster() => this.config.CoinCollector.PlayerQueue;

    public void Refresh()
    {
        if (!this.coinCollectorService.IsSessionActive()) return;

        var present = ReadPartyMembers();
        var roster  = this.config.CoinCollector.PlayerQueue;
        var changed = false;

        for (var i = roster.Count - 1; i >= 0; i--)
        {
            if (present.Any(m => NamesMatch(m.CharName, roster[i].PlayerName))) continue;
            roster.RemoveAt(i);
            changed = true;
        }

        foreach (var (charName, worldName) in present)
        {
            if (roster.Any(e => NamesMatch(e.PlayerName, charName))) continue;
            roster.Add(new CoinCollectorQueueEntry
            {
                PlayerName  = charName,
                PlayerWorld = worldName,
                JoinedAt    = DateTime.UtcNow,
            });
            changed = true;
        }

        if (changed) this.config.Save();
    }

    private void OnTurnCompleted(string playerName, int coins) => MarkPlayed(playerName, coins);

    public void MarkPlayed(string playerName, int coins)
    {
        var entry = this.config.CoinCollector.PlayerQueue
            .FirstOrDefault(e => NamesMatch(e.PlayerName, playerName));
        if (entry == null) return;
        entry.TurnsTaken++;
        entry.BestCoins = Math.Max(entry.BestCoins, coins);
        this.config.Save();
    }

    private List<(string CharName, string WorldName)> ReadPartyMembers()
    {
        var result = new List<(string, string)>();
        foreach (var member in Svc.Party)
        {
            var name = member.Name.TextValue;
            if (string.IsNullOrEmpty(name)) continue;
            if (this.playerInfo.IsHost(name)) continue;
            if (result.Any(m => NamesMatch(m.Item1, name))) continue;
            var world = member.World.ValueNullable?.Name.ToString() ?? string.Empty;
            result.Add((name, world));
        }
        return result;
    }

    private static bool NamesMatch(string left, string right) =>
        PlayerInfoService.StripWorld(left).Equals(PlayerInfoService.StripWorld(right), StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        this.coinCollectorService.TurnCompleted -= OnTurnCompleted;
    }
}
