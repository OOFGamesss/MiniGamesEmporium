using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;

/// <summary>Pushes the live Deathroll Tournament session state to GambaWhere (IPC v2) as automatic rules every thirty seconds while a session is active. Replaces the legacy MiniGamesEmporium.DeathrollTournament.GetInfo provider gate.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.IPC;

public sealed class DeathrollTournamentRules : IDisposable
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

    public DeathrollTournamentRules(
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
        var session = _config.DeathrollSession;
        if (session == null) return null;

        var tournament = _config.DeathrollTournamentSession;
        var cfg = _config.DeathrollTournament;

        var players = tournament?.PlayerCountAtStart ?? cfg.PaidPlayers.Count;
        var boostedPot = tournament?.BoostedPotAtStart ?? session.BoostedPot;
        var entryCost = tournament?.EntryCostAtStart ?? session.EntryCost;
        var totalPot = tournament != null
            ? tournament.EntryCostAtStart * tournament.PlayerCountAtStart + tournament.BoostedPotAtStart
            : entryCost * cfg.PaidPlayers.Count + boostedPot;

        var round = tournament == null
            ? "Registration"
            : tournament.CurrentRoundIndex == tournament.Rounds.Count - 1
                ? "The Final"
                : $"Round {tournament.CurrentRoundIndex + 1}";

        var payload = new GambaWhereRulesPayload();
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Game Type", Value = DeathrollGameIds.DisplayName });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Round", Value = round });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Boosted Pot", Value = boostedPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Total Pot", Value = totalPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Entry Cost", Value = entryCost });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Players Entered", Value = players });
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
