using System;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Games.VotingMadness.Utility;
using MiniGamesEmporium.IPC;
using MiniGamesEmporium.Utility;

/// <summary>Pushes live Voting Madness session state to GambaWhere over IPC.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.IPC;
public sealed class VotingMadnessRules : IDisposable
{
    private const string PluginName = "Mini Games Emporium";
    private const string Category   = "Mini Games";
    private const string Gate       = "GambaWhere.SubmitRules";
    private const int    MaxStrLen  = 50;

    private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(5);

    private readonly ICallGateSubscriber<string, string, object, bool> _submitRules;
    private readonly IFramework            _framework;
    private readonly IPluginLog            _log;
    private readonly PluginConfiguration   _config;
    private readonly VotingMadnessService  _votingMadnessService;

    private DateTime _nextPushUtc;

    public VotingMadnessRules(
        IDalamudPluginInterface pluginInterface,
        IFramework framework,
        IPluginLog log,
        PluginConfiguration config,
        VotingMadnessService votingMadnessService)
    {
        _framework             = framework;
        _log                   = log;
        _config                = config;
        _votingMadnessService  = votingMadnessService;
        _submitRules           = pluginInterface.GetIpcSubscriber<string, string, object, bool>(Gate);

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
        if (!_votingMadnessService.IsSessionActive()) return null;

        var state = _config.VotingMadnessSession!;

        var payload = new GambaWhereRulesPayload();
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Game", Value = VotingMadnessGameIds.DisplayName });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Options", Value = Truncate(_votingMadnessService.FormatOptionsList()) });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Total Votes", Value = _votingMadnessService.ComputeTotalVotes() });

        if (state.IsVotingClosed)
        {
            var (winners, votes, percent, isTie) = _votingMadnessService.GetResult();
            if (winners.Count > 0)
            {
                var winnerLabel = isTie
                    ? $"Tie: {string.Join(", ", winners)} ({votes} votes, {percent:0}%)"
                    : $"{winners[0]} ({votes} votes, {percent:0}%)";
                payload.Rules.Add(new GambaWhereRuleEntry { Label = "Winner", Value = Truncate(winnerLabel) });
            }
        }
        else
        {
            payload.Rules.Add(new GambaWhereRuleEntry { Label = "Standings", Value = Truncate(_votingMadnessService.FormatStandings()) });
            if (state.HasCloseTime)
            {
                payload.Rules.Add(new GambaWhereRuleEntry
                {
                    Label = "Closes",
                    Value = $"{ServerTimeUtil.FormatCloseLabel(state.CloseHour, state.CloseMinute)} ST",
                });
            }
        }

        return payload;
    }

    private static string Truncate(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaxStrLen) return value;
        return value[..(MaxStrLen - 1)] + "…";
    }
}
