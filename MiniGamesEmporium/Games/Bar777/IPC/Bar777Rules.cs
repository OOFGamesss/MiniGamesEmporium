using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.IPC;

/// <summary>Pushes live BAR 777 session state to GambaWhere over IPC.</summary>

namespace MiniGamesEmporium.Games.Bar777.IPC;

public sealed class Bar777Rules : IDisposable
{
    private static readonly string PluginName = GambaWhereIds.PluginNameFor(Bar777GameIds.DisplayName);
    private const string Category = GambaWhereIds.Category;
    private const string Gate = "GambaWhere.SubmitRules";

    private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(5);

    private readonly ICallGateSubscriber<string, string, object, bool> _submitRules;
    private readonly IFramework _framework;
    private readonly IPluginLog _log;
    private readonly PluginConfiguration _config;

    private DateTime _nextPushUtc;

    public Bar777Rules(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog log,
        PluginConfiguration config)
    {
        _framework = framework;
        _log = log;
        _config = config;
        _submitRules = pluginInterface.GetIpcSubscriber<string, string, object, bool>(Gate);

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
        if (!Bar777GameIds.Matches(_config.ActiveSession?.GameName)) return null;

        var bar      = _config.Bar777;
        var totalPot = Bar777SessionService.ComputeTotalPot(_config);

        var payload = new GambaWhereRulesPayload();
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Game", Value = bar.CustomName });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Total Pot", Value = totalPot });
        if (bar.BoostedPot > 0)
            payload.Rules.Add(new GambaWhereRuleEntry { Label = "Boosted Pot", Value = bar.BoostedPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Cost Per Roll", Value = bar.CostPerRoll == 0 ? "Free" : bar.CostPerRoll });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Max Rolls", Value = bar.MaxRolls });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Players Played", Value = bar.PlayersPlayed });
        if (bar.UseQueue)
            payload.Rules.Add(new GambaWhereRuleEntry { Label = "Queue", Value = _config.QueuedPlayers.Count });
        return payload;
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
    }
}
