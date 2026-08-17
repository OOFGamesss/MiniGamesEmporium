using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Models;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.IPC;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Pushes live Higher/Lower session state to GambaWhere over IPC.</summary>

namespace MiniGamesEmporium.Games.HigherLower.IPC;
public sealed class HigherLowerRules : IDisposable
{
    private static readonly string PluginName = GambaWhereIds.PluginNameFor(HigherLowerGameIds.DisplayName);
    private const string Category    = GambaWhereIds.Category;
    private const string Gate        = "GambaWhere.SubmitRules";
    private const int    MaxStrLen   = 50;

    private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(5);

    private readonly ICallGateSubscriber<string, string, object, bool> _submitRules;
    private readonly IFramework            _framework;
    private readonly IPluginLog            _log;
    private readonly PluginConfiguration   _config;
    private readonly HigherLowerService    _higherLowerService;

    private DateTime _nextPushUtc;

    public HigherLowerRules(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog log,
        PluginConfiguration config,
        HigherLowerService higherLowerService)
    {
        _framework           = framework;
        _log                 = log;
        _config              = config;
        _higherLowerService  = higherLowerService;
        _submitRules         = pluginInterface.GetIpcSubscriber<string, string, object, bool>(Gate);

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
        if (!_higherLowerService.IsSessionActive()) return null;

        var hl       = _config.HigherLower;
        var board    = hl.SessionLeaderboard;
        var totalPot = _higherLowerService.GetTotalPot();

        var topRounds        = board.Count > 0 ? board.Max(e => e.RoundsCorrect) : 0;
        var currentlyWinning = board.Count > 0 ? FormatLeaders(board, topRounds) : "--";

        var payload = new GambaWhereRulesPayload();
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Game",              Value = HigherLowerGameIds.DisplayName });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Total Pot",         Value = totalPot });
        if (hl.BoostedPot > 0)
            payload.Rules.Add(new GambaWhereRuleEntry { Label = "Boosted Pot",  Value = hl.BoostedPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Entry Cost",        Value = hl.EntryCost == 0 ? "Free" : hl.EntryCost });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Players Played",    Value = hl.PlayersPlayed });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Highest Rounds",    Value = topRounds });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Currently Winning", Value = currentlyWinning });
        return payload;
    }

    private static string FormatLeaders(List<HigherLowerLeaderboardEntry> board, int topRounds)
    {
        var leaders = board
            .Where(e => e.RoundsCorrect == topRounds)
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
