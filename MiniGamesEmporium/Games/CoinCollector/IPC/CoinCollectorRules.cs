using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Models;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.IPC;
using MiniGamesEmporium.Services;

/// <summary>Pushes live Coin Collector session state to GambaWhere over IPC.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.IPC;
public sealed class CoinCollectorRules : IDisposable
{
    private static readonly string PluginName = GambaWhereIds.PluginNameFor(CoinCollectorGameIds.DisplayName);
    private const string Category  = GambaWhereIds.Category;
    private const string Gate      = "GambaWhere.SubmitRules";
    private const int    MaxStrLen = 50;

    private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(5);

    private readonly ICallGateSubscriber<string, string, object, bool> _submitRules;
    private readonly IFramework            _framework;
    private readonly IPluginLog            _log;
    private readonly PluginConfiguration   _config;
    private readonly CoinCollectorService  _coinCollectorService;

    private DateTime _nextPushUtc;

    public CoinCollectorRules(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog log,
        PluginConfiguration config,
        CoinCollectorService coinCollectorService)
    {
        _framework            = framework;
        _log                  = log;
        _config               = config;
        _coinCollectorService = coinCollectorService;
        _submitRules          = pluginInterface.GetIpcSubscriber<string, string, object, bool>(Gate);

        _framework.Update += OnFrameworkUpdate;
        _nextPushUtc = DateTime.UtcNow;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (DateTime.UtcNow < _nextPushUtc) return;
        _nextPushUtc = DateTime.UtcNow + PushInterval;
        PushRules();
    }

    private void PushRules()
    {
        var payload = BuildPayload();
        if (payload == null) return;

        try
        {
            _submitRules.InvokeFunc(PluginName, Category, payload);
        }
        catch (Exception ex)
        {
            _log.Debug($"GambaWhere SubmitRules IPC unavailable: {ex.Message}");
        }
    }

    private GambaWhereRulesPayload? BuildPayload()
    {
        if (!_coinCollectorService.IsSessionActive()) return null;

        var cc       = _config.CoinCollector;
        var board    = cc.SessionLeaderboard;
        var totalPot = _coinCollectorService.GetTotalPot();

        var topCoins         = board.Count > 0 ? board.Max(e => e.Coins) : 0;
        var currentlyWinning = board.Count > 0 ? FormatLeaders(board, topCoins) : "--";

        var payload = new GambaWhereRulesPayload();
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Game",              Value = CoinCollectorGameIds.DisplayName });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Total Pot",         Value = totalPot });
        if (cc.BoostedPot > 0)
            payload.Rules.Add(new GambaWhereRuleEntry { Label = "Boosted Pot",   Value = cc.BoostedPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Entry Cost",        Value = cc.EntryCost == 0 ? "Free" : cc.EntryCost });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Players Played",    Value = cc.PlayersPlayed });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Most Coins",        Value = topCoins });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Currently Winning", Value = currentlyWinning });
        return payload;
    }

    private static string FormatLeaders(List<CoinCollectorLeaderboardEntry> board, int topCoins)
    {
        var leaders = board
            .Where(e => e.Coins == topCoins)
            .Select(e => e.PlayerName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (leaders.Count == 0) return "--";

        var full = string.Join(", ", leaders);
        if (full.Length <= MaxStrLen) return full;

        var stripped = string.Join(", ", leaders.Select(PlayerInfoService.StripWorld));
        if (stripped.Length <= MaxStrLen) return stripped;

        var first = PlayerInfoService.StripWorld(leaders[0]);
        if (first.Length <= MaxStrLen) return first;

        return $"{leaders.Count} Players";
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
    }
}
