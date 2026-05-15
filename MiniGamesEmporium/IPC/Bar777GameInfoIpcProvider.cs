using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

namespace MiniGamesEmporium.IPC;

public sealed class Bar777GameInfoIpcProvider : IDisposable
{
    public const string IpcKeyGetInfo = "MiniGamesEmporium.Bar777.GetInfo";

    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromMinutes(1);

    private readonly ICallGateProvider<Bar777GameInfoIpc?> _provider;
    private readonly PluginConfiguration                   _config;
    private readonly SessionService                        _sessionService;
    private readonly IFramework                            _framework;

    private DateTime _nextSnapshotUtc;

    public Bar777GameInfoIpcProvider(
        IDalamudPluginInterface pluginInterface,
        IFramework              framework,
        PluginConfiguration     config,
        SessionService          sessionService)
    {
        _config         = config;
        _sessionService = sessionService;
        _framework      = framework;

        _provider = pluginInterface.GetIpcProvider<Bar777GameInfoIpc?>(IpcKeyGetInfo);
        _provider.RegisterFunc(BuildSnapshot);

        _sessionService.SessionUpdated += OnSessionUpdated;
        _framework.Update += OnFrameworkUpdate;

        _nextSnapshotUtc = DateTime.UtcNow + SnapshotInterval;
    }

    public void Dispose()
    {
        _framework.Update -= OnFrameworkUpdate;
        _sessionService.SessionUpdated -= OnSessionUpdated;
        _provider.UnregisterFunc();
    }

    private void OnSessionUpdated()
    {
        _nextSnapshotUtc = DateTime.UtcNow + SnapshotInterval;
        LogAndNotify(BuildSnapshot());
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (DateTime.UtcNow < _nextSnapshotUtc) return;
        _nextSnapshotUtc = DateTime.UtcNow + SnapshotInterval;

        var snapshot = BuildSnapshot();
        if (snapshot == null) return;
        LogAndNotify(snapshot);
    }

    private void LogAndNotify(Bar777GameInfoIpc? snapshot)
    {
        if (snapshot != null)
            MiniGamesEmporium.Log.Information(
                "[IPC] {Label} | BoostedPot={BoostedPot} TotalPot={TotalPot} CostPerRoll={CostPerRoll} PlayersPlayed={PlayersPlayed} Queue={Queue}",
                snapshot.GameLabel, snapshot.BoostedPot, snapshot.TotalPot, snapshot.CostPerRoll, snapshot.PlayersPlayed, snapshot.Queue?.ToString() ?? "N/A");
        else
            MiniGamesEmporium.Log.Information("[IPC] Bar777 session ended, notifying subscribers.");

        _provider.SendMessage();
    }

    private Bar777GameInfoIpc? BuildSnapshot() => Bar777GameInfoIpc.FromConfig(_config);
}
