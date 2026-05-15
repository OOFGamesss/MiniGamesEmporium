using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.IPC;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.State;
using MiniGamesEmporium.UI;
using System.Collections.Generic;

/// <summary>Plugin entry point that initialises all services, registers the main and config slash commands, and manages the Dalamud window system lifecycle.</summary>

namespace MiniGamesEmporium;
public sealed class MiniGamesEmporium : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    private const string MainCommandFull = "/minigamesemporium";
    private const string MainCommandShort = "/mge";
    private const string ConfigCommand = "/mgeconfig";
    public PluginConfiguration Configuration { get; init; }
    private readonly WindowSystem windowSystem = new("MiniGamesEmporium");
    private readonly MainWindow mainWindow;
    private readonly SessionService sessionService;
    private readonly ChatListener chatListener;
    private readonly ChatQueueService chatQueueService;
    private readonly WindowOpenedIpc windowOpenedIpc;
    private readonly Bar777GameInfoIpcProvider bar777IpcProvider;
    public MiniGamesEmporium()
    {
        ECommonsMain.Init(PluginInterface, this);
        Configuration = PluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        Configuration.QueueJoinChannels ??= new QueueJoinChannelsConfig();
        Configuration.Bar777.Chat ??= new Bar777ChatConfig();
        Configuration.SessionHistory ??= new List<SessionRecord>();
        chatQueueService = new ChatQueueService();
        sessionService = new SessionService(Configuration);
        chatListener = new ChatListener(ChatGui, Configuration, sessionService, Log, ObjectTable);
        mainWindow = new MainWindow(Configuration, sessionService, chatQueueService);
        windowSystem.AddWindow(mainWindow);
        windowOpenedIpc = new WindowOpenedIpc(PluginInterface, mainWindow);
        bar777IpcProvider = new Bar777GameInfoIpcProvider(PluginInterface, Framework, Configuration, sessionService);
        CommandManager.AddHandler(MainCommandFull, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Mini Games Emporium window.",
        });
        CommandManager.AddHandler(MainCommandShort, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Mini Games Emporium window.",
        });
        CommandManager.AddHandler(ConfigCommand, new CommandInfo(OnConfigCommand)
        {
            HelpMessage = "Opens the Mini Games Emporium settings tab.",
        });
        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        Log.Information("Plugin loaded.");
    }
    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        windowOpenedIpc.Dispose();
        bar777IpcProvider.Dispose();
        chatListener.Dispose();
        chatQueueService.Dispose();
        windowSystem.RemoveAllWindows();
        mainWindow.Dispose();
        CommandManager.RemoveHandler(MainCommandFull);
        CommandManager.RemoveHandler(MainCommandShort);
        CommandManager.RemoveHandler(ConfigCommand);
        ECommonsMain.Dispose();
    }
    private void OnCommand(string command, string args) => ToggleMainUi();
    public void ToggleMainUi() => mainWindow.Toggle();
    private void OnConfigCommand(string command, string args) => OpenConfigUi();
    public void OpenConfigUi() => mainWindow.OpenSettingsTab();
}
