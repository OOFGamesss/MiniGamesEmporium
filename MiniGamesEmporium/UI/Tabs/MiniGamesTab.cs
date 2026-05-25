using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.UI;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.UI;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Games.MinefieldGambit.UI;
using MiniGamesEmporium.Games.GamblerDerby.UI;
using MiniGamesEmporium.Games.HotShots.UI;
using MiniGamesEmporium.Games.BeerPong.UI;
using MiniGamesEmporium.Games.Darts.UI;
using MiniGamesEmporium.Games.EightBallPool.UI;
using MiniGamesEmporium.Games.RussianRoulette.UI;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Draws the Mini Games tab with themed sub-tabs for BAR 777, Deathroll Tournament, Minefield Gambit, Gambler Derby, Hot Shots, Beer Pong, Darts, 8 Ball Pool, and Russian Roulette, and displays a centred win alert popup when a winning roll is detected.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class MiniGamesTab : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly Bar777Panel bar777Panel;
    private readonly DeathrollTournamentPanel deathrollTournamentPanel;
    private readonly MinefieldGambitPanel minefieldGambitPanel;
    private readonly GamblerDerbyPanel gamblerDerbyPanel;
    private readonly HotShotsPanel hotShotsPanel;
    private readonly BeerPongPanel beerPongPanel;
    private readonly DartsPanel dartsPanel;
    private readonly EightBallPoolPanel eightBallPoolPanel;
    private readonly RussianRoulettePanel russianRoulettePanel;
    private bool showWinAlert = false;
    private string winAlertPlayer = string.Empty;
    private int winAlertRoll = 0;
    private readonly DeathrollTournamentService deathrollService;
    private readonly SessionService sessionService;
    public MiniGamesTab(
        PluginConfiguration config,
        SessionService sessionService,
        ChatQueueService chatQueue,
        DeathrollTournamentService deathrollService,
        DeathrollDiscordWebhookService deathrollDiscordService,
        IPluginLog log)
    {
        this.config                   = config;
        this.deathrollService         = deathrollService;
        this.sessionService           = sessionService;
        this.bar777Panel              = new Bar777Panel(config, sessionService, chatQueue, deathrollService);
        this.deathrollTournamentPanel = new DeathrollTournamentPanel(config, deathrollService, deathrollDiscordService, chatQueue, sessionService, log);
        this.minefieldGambitPanel = new MinefieldGambitPanel();
        this.gamblerDerbyPanel = new GamblerDerbyPanel();
        this.hotShotsPanel = new HotShotsPanel();
        this.beerPongPanel = new BeerPongPanel();
        this.dartsPanel = new DartsPanel();
        this.eightBallPoolPanel = new EightBallPoolPanel();
        this.russianRoulettePanel = new RussianRoulettePanel();
        sessionService.WinDetected += OnWinDetected;
    }
    private void OnWinDetected(string playerName, int roll)
    {
        this.showWinAlert = true;
        this.winAlertPlayer = playerName;
        this.winAlertRoll = roll;
    }
    public void Dispose()
    {
        this.sessionService.WinDetected -= OnWinDetected;
        this.bar777Panel.Dispose();
        this.deathrollTournamentPanel.Dispose();
    }
    public void Draw()
    {
        DrawWinAlertPopup();
        ImGui.Spacing();
        using var tabBar = ImRaii.TabBar("##MGE_GameTabBar_v4");
        if (!tabBar.Success) return;
        DrawBar777Tab();
        DrawDeathrollTournamentTab();
        DrawMinefieldGambitTab();
        DrawGamblerDerbyTab();
        DrawHotShotsTab();
        DrawBeerPongTab();
        DrawDartsTab();
        DrawEightBallPoolTab();
        DrawRussianRouletteTab();
    }
    private void DrawBar777Tab()
    {
        using var chrome = new EmporiumNeonTheme.Bar777TabItemScope();
        using var tab = ImRaii.TabItem(this.config.Bar777.CustomName);
        if (!tab.Success) return;
        this.bar777Panel.Draw();
    }
    private void DrawDeathrollTournamentTab()
    {
        using var chrome = new EmporiumNeonTheme.DeathrollTournamentTabItemScope();
        using var tab = ImRaii.TabItem("Deathroll Tournament");
        if (!tab.Success) return;
        this.deathrollTournamentPanel.Draw();
    }
    private void DrawMinefieldGambitTab()
    {
        using var chrome = new EmporiumNeonTheme.MinefieldGambitTabItemScope();
        using var tab = ImRaii.TabItem("Minefield Gambit");
        if (!tab.Success) return;
        this.minefieldGambitPanel.Draw();
    }
    private void DrawGamblerDerbyTab()
    {
        using var chrome = new EmporiumNeonTheme.GamblerDerbyTabItemScope();
        using var tab = ImRaii.TabItem("Gambler Derby");
        if (!tab.Success) return;
        this.gamblerDerbyPanel.Draw();
    }
    private void DrawHotShotsTab()
    {
        using var chrome = new EmporiumNeonTheme.HotShotsTabItemScope();
        using var tab = ImRaii.TabItem("Hot Shots");
        if (!tab.Success) return;
        this.hotShotsPanel.Draw();
    }
    private void DrawBeerPongTab()
    {
        using var chrome = new EmporiumNeonTheme.BeerPongTabItemScope();
        using var tab = ImRaii.TabItem("Beer Pong");
        if (!tab.Success) return;
        this.beerPongPanel.Draw();
    }
    private void DrawDartsTab()
    {
        using var chrome = new EmporiumNeonTheme.DartsTabItemScope();
        using var tab = ImRaii.TabItem("Darts");
        if (!tab.Success) return;
        this.dartsPanel.Draw();
    }
    private void DrawEightBallPoolTab()
    {
        using var chrome = new EmporiumNeonTheme.EightBallPoolTabItemScope();
        using var tab = ImRaii.TabItem("8 Ball Pool");
        if (!tab.Success) return;
        this.eightBallPoolPanel.Draw();
    }
    private void DrawRussianRouletteTab()
    {
        using var chrome = new EmporiumNeonTheme.RussianRouletteTabItemScope();
        using var tab = ImRaii.TabItem("Russian Roulette");
        if (!tab.Success) return;
        this.russianRoulettePanel.Draw();
    }
    private void DrawWinAlertPopup()
    {
        if (!this.showWinAlert) return;
        ImGui.OpenPopup("##WinAlert");
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.Popup("##WinAlert", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.WinGold, "WINNER!");
        ImGui.Separator();
        ImGui.Text($"{this.winAlertPlayer} rolled {this.winAlertRoll}!");
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Dismiss", "##WinDismiss"))
        {
            this.showWinAlert = false;
            ImGui.CloseCurrentPopup();
        }
    }
}
