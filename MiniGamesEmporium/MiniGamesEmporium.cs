using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using MiniGamesEmporium.API;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Events;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.Bar777.Events;
using MiniGamesEmporium.Games.Bar777.IPC;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.CoinCollector.Events;
using MiniGamesEmporium.Games.CoinCollector.IPC;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Discord;
using MiniGamesEmporium.Games.DeathrollTournament.Events;
using MiniGamesEmporium.Games.DeathrollTournament.IPC;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Games.DeathrollTournament.Webview;
using MiniGamesEmporium.Games.HigherLower.Events;
using MiniGamesEmporium.Games.HigherLower.IPC;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.Games.Raffle.Events;
using MiniGamesEmporium.Games.Raffle.IPC;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.Games.VotingMadness.Events;
using MiniGamesEmporium.Games.VotingMadness.IPC;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Games.VotingMadness.Utility;
using MiniGamesEmporium.IPC;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.UI;
using System;
using System.Collections.Generic;

/// <summary>Plugin entry point that wires up the services, commands, and window system.</summary>

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
    [PluginService] internal static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    private const string MainCommandFull = "/minigamesemporium";
    private const string MainCommandShort = "/mge";
    private const string ConfigCommand = "/mgeconfig";
    public PluginConfiguration Configuration { get; init; }
    private readonly WindowSystem windowSystem = new("MiniGamesEmporium");
    private readonly MainWindow mainWindow;
    private readonly HistoryService historyService;
    private readonly Bar777SessionService bar777SessionService;
    private readonly SessionService sessionService;
    private readonly PlayerInfoService playerInfoService;
    private readonly RollService rollService;
    private readonly PresetService presetService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly DeathrollBettingService deathrollBettingService;
    private readonly DeathrollWebhookService deathrollDiscordService;
    private readonly MgeApiClient mgeApiClient;
    private readonly DrtWebviewService drtWebviewService;
    private readonly HigherLowerService higherLowerService;
    private readonly RaffleService raffleService;
    private readonly VotingMadnessService votingMadnessService;
    private readonly CoinCollectorService coinCollectorService;
    private readonly ChatListenerService chatListener;
    private readonly TradeListenerService tradeListenerService;
    private readonly ChatQueueService chatQueueService;
    private readonly AutoPayoutService autoPayoutService;
    private readonly WindowOpenedIpc windowOpenedIpc;
    private readonly Bar777Rules bar777Rules;
    private readonly DeathrollTournamentRules deathrollRules;
    private readonly HigherLowerRules higherLowerRules;
    private readonly RaffleRules raffleRules;
    private readonly VotingMadnessRules votingMadnessRules;
    private readonly CoinCollectorRules coinCollectorRules;
    private readonly PlayerContextMenuHandler playerContextMenuHandler;
    private readonly ChatPlayerContextMenuHandler chatPlayerContextMenuHandler;
    private readonly Action<string, string, int, int> _onMatchWon;
    private readonly Action<string, int, int, int>    _onGameWon;
    private readonly Action<string, long>             _onTournamentWon;
    public MiniGamesEmporium()
    {
        ECommonsMain.Init(PluginInterface, this);
        Configuration = PluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        Configuration.QueueJoinChannels ??= new QueueConfig();
        Configuration.Bar777.Chat ??= new Bar777ChatConfig();
        Configuration.Raffle ??= new();
        Configuration.Raffle.Chat ??= new();
        Configuration.Raffle.JoinChannels ??= new QueueConfig();
        Configuration.CoinCollector ??= new();
        Configuration.CoinCollector.Chat ??= new();
        Configuration.CoinCollector.PlayerQueue ??= new();
        Configuration.CoinCollector.Attempts ??= new();
        Configuration.VotingMadness ??= new();
        Configuration.VotingMadness.Chat ??= new();
        Configuration.VotingMadness.VoteChannels ??= new QueueConfig();
        Configuration.VotingMadness.Options ??= new() { "Yes", "No" };
        var optionsBefore = string.Join('\0', Configuration.VotingMadness.Options);
        Configuration.VotingMadness.SanitiseOptions();
        if (!string.Equals(optionsBefore, string.Join('\0', Configuration.VotingMadness.Options), StringComparison.Ordinal))
            Configuration.Save();
        Configuration.SessionHistory ??= new();
        Configuration.Webview ??= new();
        Configuration.Transactions ??= new();
        MigrateDeathrollChatConfig();
        MigrateHigherLowerActiveSession();
        historyService = new HistoryService(PluginInterface, Configuration);
        chatQueueService = new ChatQueueService();
        sessionService = new SessionService();
        bar777SessionService = new Bar777SessionService(Configuration, historyService);
        playerInfoService = new PlayerInfoService();
        rollService = new RollService(playerInfoService);
        higherLowerService = new HigherLowerService(Configuration, historyService, ChatGui, sessionService);
        presetService = new PresetService(Configuration);
        deathrollService = new DeathrollTournamentService(Configuration, historyService);
        deathrollBettingService = new DeathrollBettingService(Configuration, historyService, deathrollService);
        raffleService = new RaffleService(Configuration, historyService);
        votingMadnessService = new VotingMadnessService(Configuration, historyService);
        coinCollectorService = new CoinCollectorService(Configuration, historyService);
        deathrollDiscordService = new DeathrollWebhookService(Log, Configuration, PluginInterface.AssemblyLocation.DirectoryName!);
        deathrollService.SessionUpdated += deathrollDiscordService.TriggerSync;
        _onMatchWon      = (_, _, _, _) => deathrollDiscordService.TriggerSync();
        _onGameWon       = (_, _, _, _) => deathrollDiscordService.TriggerSync();
        _onTournamentWon = (_, _)       => deathrollDiscordService.TriggerSync();
        deathrollService.MatchWon      += _onMatchWon;
        deathrollService.GameWon       += _onGameWon;
        deathrollService.TournamentWon += _onTournamentWon;
        mgeApiClient = new MgeApiClient(() => Configuration.Webview.ApiHostKey, Log);
        drtWebviewService = new DrtWebviewService(Configuration, deathrollService, mgeApiClient, Framework, Log);
        drtWebviewService.WebSessionChanged += deathrollDiscordService.TriggerSync;
        sessionService.RegisterGame(Bar777GameIds.DisplayName, bar777SessionService.IsActive);
        sessionService.RegisterGame(HigherLowerGameIds.DisplayName, higherLowerService.IsSessionActive);
        sessionService.RegisterGame(DeathrollGameIds.DisplayName, deathrollService.IsSessionActive);
        sessionService.RegisterGame(RaffleGameIds.DisplayName, raffleService.IsSessionActive);
        sessionService.RegisterGame(VotingMadnessGameIds.DisplayName, votingMadnessService.IsSessionActive);
        sessionService.RegisterGame(CoinCollectorGameIds.DisplayName, coinCollectorService.IsSessionActive);
        playerContextMenuHandler = new PlayerContextMenuHandler(ContextMenu);
        playerContextMenuHandler.Register(new PlayerContextMenuEntry
        {
            Label      = "Add to Deathroll Tournament",
            IsVisible  = () => deathrollService.IsSessionActive() && !deathrollService.HasActiveTournament(),
            OnSelected = deathrollService.AddPlayer,
        });
        playerContextMenuHandler.Register(new PlayerContextMenuEntry
        {
            Label      = "Add to BAR 777 Queue",
            IsVisible  = () => { var s = bar777SessionService.GetActiveSession(); return s != null && Bar777GameIds.Matches(s.GameName) && Configuration.Bar777.UseQueue; },
            OnSelected = bar777SessionService.TryEnqueuePlayer,
        });
        playerContextMenuHandler.Register(new PlayerContextMenuEntry
        {
            Label      = "Add to Raffle",
            IsVisible  = raffleService.IsSessionActive,
            OnSelected = raffleService.AddPlayer,
        });
        chatPlayerContextMenuHandler = new ChatPlayerContextMenuHandler(ContextMenu);
        chatPlayerContextMenuHandler.Register(new PlayerContextMenuEntry
        {
            Label      = "Add to Deathroll Tournament",
            IsVisible  = () => deathrollService.IsSessionActive() && !deathrollService.HasActiveTournament(),
            OnSelected = deathrollService.AddPlayer,
        });
        chatPlayerContextMenuHandler.Register(new PlayerContextMenuEntry
        {
            Label      = "Add to BAR 777 Queue",
            IsVisible  = () => { var s = bar777SessionService.GetActiveSession(); return s != null && Bar777GameIds.Matches(s.GameName) && Configuration.Bar777.UseQueue; },
            OnSelected = bar777SessionService.TryEnqueuePlayer,
        });
        chatPlayerContextMenuHandler.Register(new PlayerContextMenuEntry
        {
            Label      = "Add to Raffle",
            IsVisible  = raffleService.IsSessionActive,
            OnSelected = raffleService.AddPlayer,
        });
        chatListener = new ChatListenerService(
            ChatGui,
            sessionService,
            new IChatRollHandler[]    { new Bar777RollHandler(Configuration, bar777SessionService), new DeathrollRollHandler(deathrollService, playerInfoService), new HigherLowerRollHandler(Configuration, higherLowerService, playerInfoService), new RaffleRollHandler(raffleService, playerInfoService), new CoinCollectorRollHandler(coinCollectorService) },
            new IChatKeywordHandler[] { new Bar777KeywordHandler(Configuration, bar777SessionService, playerInfoService), new DeathrollKeywordHandler(Configuration, deathrollService, playerInfoService), new DeathrollBetKeywordHandler(Configuration, deathrollService, deathrollBettingService, playerInfoService), new HigherLowerKeywordHandler(), new RaffleKeywordHandler(Configuration, raffleService, playerInfoService), new VotingMadnessKeywordHandler(Configuration, votingMadnessService, playerInfoService), new CoinCollectorKeywordHandler() },
            rollService,
            Log);
        tradeListenerService  = new TradeListenerService(bar777SessionService, sessionService, deathrollService, deathrollBettingService, higherLowerService, raffleService, coinCollectorService, Log);
        autoPayoutService     = new AutoPayoutService(chatQueueService, Log);
        mainWindow = new MainWindow(Configuration, bar777SessionService, sessionService, chatQueueService, deathrollService, deathrollBettingService, deathrollDiscordService, drtWebviewService, presetService, Log, historyService, autoPayoutService, higherLowerService, playerInfoService, raffleService, votingMadnessService, coinCollectorService);
        windowSystem.AddWindow(mainWindow);
        windowOpenedIpc = new WindowOpenedIpc(PluginInterface, Log, mainWindow);
        bar777Rules = new Bar777Rules(PluginInterface, Framework, Log, Configuration);
        deathrollRules = new DeathrollTournamentRules(PluginInterface, Framework, Log, Configuration, deathrollBettingService);
        higherLowerRules = new HigherLowerRules(PluginInterface, Framework, Log, Configuration, higherLowerService);
        raffleRules = new RaffleRules(PluginInterface, Framework, Log, Configuration);
        votingMadnessRules = new VotingMadnessRules(PluginInterface, Framework, Log, Configuration, votingMadnessService);
        coinCollectorRules = new CoinCollectorRules(PluginInterface, Framework, Log, Configuration, coinCollectorService);

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

    private void MigrateHigherLowerActiveSession()
    {
        var session = Configuration.ActiveSession;
        if (session == null || !HigherLowerGameIds.Matches(session.GameName)) return;
        Configuration.HigherLowerActiveSession = session;
        Configuration.ActiveSession = null;
        Configuration.Save();
    }

    private void MigrateDeathrollChatConfig()
    {
        const int currentVersion = 2;
        var cfg = Configuration.DeathrollTournament;
        if (cfg.Chat.ConfigVersion >= currentVersion) return;
        cfg.Chat = new DeathrollTournamentChatConfig { ConfigVersion = currentVersion };
        Configuration.Save();
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        deathrollService.SessionUpdated -= deathrollDiscordService.TriggerSync;
        deathrollService.MatchWon      -= _onMatchWon;
        deathrollService.GameWon       -= _onGameWon;
        deathrollService.TournamentWon -= _onTournamentWon;
        deathrollBettingService.Dispose();
        drtWebviewService.WebSessionChanged -= deathrollDiscordService.TriggerSync;
        deathrollDiscordService.Dispose();
        drtWebviewService.Dispose();
        mgeApiClient.Dispose();
        playerContextMenuHandler.Dispose();
        chatPlayerContextMenuHandler.Dispose();
        windowOpenedIpc.Dispose();
        bar777Rules.Dispose();
        deathrollRules.Dispose();
        higherLowerRules.Dispose();
        raffleRules.Dispose();
        votingMadnessRules.Dispose();
        coinCollectorRules.Dispose();
        chatListener.Dispose();
        tradeListenerService.Dispose();
        higherLowerService.Dispose();
        autoPayoutService.Dispose();
        chatQueueService.Dispose();
        historyService.Dispose();
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
