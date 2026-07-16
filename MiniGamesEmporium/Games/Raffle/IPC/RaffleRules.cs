using System;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.IPC;
using MiniGamesEmporium.Services;

/// <summary>Pushes live Raffle session state to GambaWhere over IPC.</summary>

namespace MiniGamesEmporium.Games.Raffle.IPC;
public sealed class RaffleRules : IDisposable
{
    private const string PluginName = "Mini Games Emporium";
    private const string Category = "Mini Games";
    private const string Gate = "GambaWhere.SubmitRules";

    private static readonly TimeSpan PushInterval = TimeSpan.FromSeconds(5);

    private readonly ICallGateSubscriber<string, string, object, bool> _submitRules;
    private readonly IFramework _framework;
    private readonly IPluginLog _log;
    private readonly PluginConfiguration _config;

    private DateTime _nextPushUtc;

    public RaffleRules(
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
        var state = _config.RaffleSession;
        if (state == null) return null;

        var ticketsSold = state.Blocks.Sum(b => b.Count);
        var totalPot    = RaffleService.ComputeTotalPot(_config);

        var closing = state.HasCloseTime
            ? $"{RaffleTimeUtil.FormatCloseLabel(state.CloseHour, state.CloseMinute)} ST"
            : "TBA";

        var payload = new GambaWhereRulesPayload();
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Game", Value = RaffleGameIds.DisplayName });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Total Pot", Value = totalPot });
        if (state.BoostedPotAtStart > 0)
            payload.Rules.Add(new GambaWhereRuleEntry { Label = "Boosted Pot", Value = state.BoostedPotAtStart });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Ticket Cost", Value = state.TicketCostAtStart == 0 ? "Free" : state.TicketCostAtStart });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Max Tickets Per Person", Value = state.MaxTicketsPerPlayerAtStart });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Players", Value = RaffleService.ComputePlayersWithTickets(_config) });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Tickets Sold", Value = ticketsSold });
        payload.Rules.Add(new GambaWhereRuleEntry { Label = "Closes", Value = closing });
        if (state.WinnerName != null && state.WinningNumber != null)
            payload.Rules.Add(new GambaWhereRuleEntry { Label = "Winner", Value = $"#{state.WinningNumber} {PlayerInfoService.StripWorld(state.WinnerName)}" });
        return payload;
    }
}
