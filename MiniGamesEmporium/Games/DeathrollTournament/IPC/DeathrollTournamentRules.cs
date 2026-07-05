using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.IPC;

/// <summary>Pushes live Deathroll Tournament session state to GambaWhere over IPC.</summary>

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

        var round = tournament == null ? "Registration" : tournament.CurrentRoundLabel();

        var payload = new GambaWhereRulesPayload();
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Game", Value = DeathrollGameIds.DisplayName });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Round", Value = round });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Boosted Pot", Value = boostedPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Total Pot", Value = totalPot });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Entry Cost", Value = entryCost == 0 ? "Free" : entryCost });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Players Entered", Value = players });
        return payload;
    }
}
