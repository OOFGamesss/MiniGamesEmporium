using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using MiniGamesEmporium.UI;

/// <summary>Fires an IPC message over the MiniGamesEmporium.WindowOpened gate whenever the main plugin window is opened.</summary>

namespace MiniGamesEmporium.IPC;

public sealed class WindowOpenedIpc : IDisposable
{
    private const string IpcKey = "MiniGamesEmporium.WindowOpened";

    private readonly ICallGateProvider<object> _provider;
    private readonly MainWindow _mainWindow;

    public WindowOpenedIpc(IDalamudPluginInterface pluginInterface, MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _provider = pluginInterface.GetIpcProvider<object>(IpcKey);
        _mainWindow.WindowOpened += OnWindowOpened;
    }

    public void Dispose()
    {
        _mainWindow.WindowOpened -= OnWindowOpened;
    }

    private void OnWindowOpened() => _provider.SendMessage();
}
