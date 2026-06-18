using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.UI;

/// <summary>Notifies GambaWhere (IPC v2) in real time whenever the Mini Games Emporium window opens, so it can offer to start a session. Replaces the legacy MiniGamesEmporium.WindowOpened provider gate.</summary>

namespace MiniGamesEmporium.IPC;

public sealed class WindowOpenedIpc : IDisposable
{
    private const string PluginName = "Mini Games Emporium";
    private const string Category = "Mini Games";
    private const string Gate = "GambaWhere.WindowOpened";

    private readonly ICallGateSubscriber<string, string, bool> _windowOpened;
    private readonly IPluginLog _log;
    private readonly MainWindow _mainWindow;

    public WindowOpenedIpc(IDalamudPluginInterface pluginInterface, IPluginLog log, MainWindow mainWindow)
    {
        _log = log;
        _mainWindow = mainWindow;
        _windowOpened = pluginInterface.GetIpcSubscriber<string, string, bool>(Gate);
        _mainWindow.WindowOpened += OnWindowOpened;
    }

    public void Dispose()
    {
        _mainWindow.WindowOpened -= OnWindowOpened;
    }

    private void OnWindowOpened()
    {
        try
        {
            _windowOpened.InvokeFunc(PluginName, Category);
        }
        catch (Exception ex)
        {
            _log.Debug($"GambaWhere WindowOpened IPC unavailable: {ex.Message}");
        }
    }
}
