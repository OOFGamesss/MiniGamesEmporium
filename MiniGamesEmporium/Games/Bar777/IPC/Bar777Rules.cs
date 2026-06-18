using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;

/// <summary>Pushes the live BAR 777 session state to GambaWhere (IPC v2) as automatic rules every thirty seconds while a session is active. Replaces the legacy MiniGamesEmporium.Bar777.GetInfo provider gate.</summary>

namespace MiniGamesEmporium.Games.Bar777.IPC;

public sealed class Bar777Rules : IDisposable
{
    private const string PluginName = "Mini Games Emporium";
    private const string Category = "Mini Games";
    private const string Gate = "GambaWhere.SubmitRules";

    private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(30);

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

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
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
        if (_config.ActiveSession == null) return null;

        var bar = _config.Bar777;
        var totalPot = bar.BoostedPot + (bar.AddTradesToPot ? bar.SessionTradedTotal : 0L);

        var payload = new GambaWhereRulesPayload();
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Game Type", Value = bar.CustomName });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Boosted Pot", Value = bar.BoostedPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Total Pot", Value = totalPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Cost Per Roll", Value = bar.CostPerRoll });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Max Rolls", Value = bar.MaxRolls });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Players Played", Value = bar.PlayersPlayed });
        if (bar.UseQueue)
            payload.Rules.Add(new GambaWhereRuleEntry { Label = "Queue", Value = _config.QueuedPlayers.Count });
        return payload;
    }
}

/// <summary>
/// Mirror of GambaWhere's IPC v2 rules contract. Property names (Rules / Label / Value) must match
/// what GambaWhere reflects on receipt. Value must be a string, bool, int, long or double; a maximum
/// of ten entries is accepted.
/// </summary>
public sealed class GambaWhereRulesPayload
{
    public List<GambaWhereRuleEntry> Rules { get; set; } = new();
}

public sealed class GambaWhereRuleEntry
{
    public string Label { get; set; } = string.Empty;

    public object? Value { get; set; }
}
