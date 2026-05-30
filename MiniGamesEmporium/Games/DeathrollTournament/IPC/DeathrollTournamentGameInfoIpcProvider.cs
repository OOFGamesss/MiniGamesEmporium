using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using System;

/// <summary>Registers the Deathroll Tournament IPC gate and pushes updated snapshots to subscribers on session changes and periodic framework ticks.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.IPC;

public sealed class DeathrollTournamentGameInfoIpcProvider : IDisposable
{
    public const string IpcKeyGetInfo = "MiniGamesEmporium.DeathrollTournament.GetInfo";

    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromMinutes(1);

    private readonly ICallGateProvider<DeathrollTournamentGameInfoIpc?> _provider;
    private readonly PluginConfiguration                                _config;
    private readonly DeathrollTournamentService                         _deathrollService;
    private readonly IFramework                                         _framework;

    private DateTime _nextSnapshotUtc;

    public DeathrollTournamentGameInfoIpcProvider(
        IDalamudPluginInterface    pluginInterface,
        IFramework                 framework,
        PluginConfiguration        config,
        DeathrollTournamentService deathrollService)
    {
        _config           = config;
        _deathrollService = deathrollService;
        _framework        = framework;

        _provider = pluginInterface.GetIpcProvider<DeathrollTournamentGameInfoIpc?>(IpcKeyGetInfo);
        _provider.RegisterFunc(BuildSnapshot);

        _deathrollService.SessionUpdated += OnSessionUpdated;
        _framework.Update += OnFrameworkUpdate;

        _nextSnapshotUtc = DateTime.UtcNow + SnapshotInterval;
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
        _deathrollService.SessionUpdated -= OnSessionUpdated;
        _provider.UnregisterFunc();
    }

    private void OnSessionUpdated() => LogAndNotify(BuildSnapshot());

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (DateTime.UtcNow < _nextSnapshotUtc) return;
        _nextSnapshotUtc = DateTime.UtcNow + SnapshotInterval;

        var snapshot = BuildSnapshot();
        if (snapshot == null) return;
        LogAndNotify(snapshot);
    }

    private void LogAndNotify(DeathrollTournamentGameInfoIpc? snapshot)
    {
        if (snapshot != null)
            MiniGamesEmporium.Log.Information(
                "[IPC] {Label} | Round={Round} BoostedPot={BoostedPot} TotalPot={TotalPot} EntryCost={EntryCost} PlayersEntered={PlayersEntered}",
                snapshot.GameLabel, snapshot.Round, snapshot.BoostedPot, snapshot.TotalPot, snapshot.EntryCost, snapshot.PlayersEntered);
        else
            MiniGamesEmporium.Log.Information("[IPC] DeathrollTournament session ended, notifying subscribers.");

        _provider.SendMessage();
    }

    private DeathrollTournamentGameInfoIpc? BuildSnapshot() => DeathrollTournamentGameInfoIpc.FromConfig(_config);
}
