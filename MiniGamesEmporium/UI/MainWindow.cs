using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Discord;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.UI.Tabs;
using System;
using System.Numerics;


/// <summary>The main plugin window hosting the top-level tab bar and stop-session button.</summary>

namespace MiniGamesEmporium.UI;
public sealed class MainWindow : Window, IDisposable
{
    public event Action? WindowOpened;

    private readonly TransactionHistoryTab transactionHistoryTab;
    private readonly SessionHistoryTab sessionHistoryTab;
    private readonly MiniGamesTab miniGamesTab;
    private readonly PresetManagerTab presetManagerTab;
    private readonly SettingsTab settingsTab;
    private readonly SupportTab supportTab;
    private readonly Bar777SessionService bar777SessionService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly HigherLowerService higherLowerService;
    private readonly PluginConfiguration config;
    private bool focusSettingsTab;
    private bool pendingStopConfirm;
    private bool pendingDeathrollStopConfirm;
    private bool pendingHLStopConfirm;
    private int prePushedColours;
    private bool miniGamesTabActive = true;

    public override void OnOpen() => WindowOpened?.Invoke();

    public void OpenSettingsTab()
    {
        IsOpen = true;
        focusSettingsTab = true;
    }
    public MainWindow(
        PluginConfiguration config,
        Bar777SessionService bar777SessionService,
        SessionService sessionService,
        ChatQueueService chatQueue,
        DeathrollTournamentService deathrollService,
        DeathrollWebhookService deathrollDiscordService,
        PresetService presetService,
        IPluginLog log,
        HistoryService historyService,
        AutoPayoutService autoPayoutService,
        HigherLowerService higherLowerService,
        PlayerInfoService playerInfoService)
        : base("Mini Games Emporium##MGE_Main_v2")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 440),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        this.transactionHistoryTab = new TransactionHistoryTab(historyService);
        this.sessionHistoryTab     = new SessionHistoryTab(historyService);
        this.miniGamesTab          = new MiniGamesTab(config, bar777SessionService, sessionService, chatQueue, deathrollService, deathrollDiscordService, log, historyService, autoPayoutService, higherLowerService, playerInfoService);
        this.presetManagerTab      = new PresetManagerTab(config, presetService);
        this.settingsTab           = new SettingsTab(config);
        this.supportTab            = new SupportTab();
        this.bar777SessionService  = bar777SessionService;
        this.deathrollService      = deathrollService;
        this.higherLowerService    = higherLowerService;
        this.config                = config;
    }
    public void Dispose()
    {
        this.miniGamesTab.Dispose();
    }
    public override void PreDraw()
    {
        Flags = this.miniGamesTabActive
            ? ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
            : ImGuiWindowFlags.None;
        var deep = new Vector4(0.04f, 0.02f, 0.07f, 0.97f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg,          deep);
        ImGui.PushStyleColor(ImGuiCol.TitleBg,           deep);
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive,     EmporiumNeonTheme.MainTabPurpleActive);
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed,  deep);
        ImGui.PushStyleColor(ImGuiCol.Border,            new Vector4(0.42f, 0.08f, 0.62f, 0.60f));
        this.prePushedColours = 5;
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(this.prePushedColours);
    }

    public override void Draw()
    {
        using var theme = new EmporiumNeonTheme.Scope();
        using var mainTabChrome = new EmporiumNeonTheme.MainWindowTabChromeScope();
        var tabBarRowScreenY  = ImGui.GetCursorScreenPos().Y;
        var tabBarContentY    = tabBarRowScreenY;
        {
            using var tabBar = ImRaii.TabBar("##MGE_TabBar_v4");
            if (!tabBar.Success)
                return;
            tabBarContentY = ImGui.GetCursorScreenPos().Y;
            DrawMiniGamesTab();
            DrawSessionHistoryTab();
            DrawTransactionHistoryTab();
            DrawPresetManagerTab();
            DrawSettingsTab();
            DrawSupportTab();
        }
        DrawBar777StopSessionMainTabRowButton(tabBarRowScreenY, tabBarContentY);
        DrawDeathrollStopButton(tabBarRowScreenY, tabBarContentY);
        DrawHigherLowerStopButton(tabBarRowScreenY, tabBarContentY);
        DrawStopSessionConfirmPopup();
        DrawDeathrollStopConfirmPopup();
        DrawHLStopConfirmPopup();
    }
    private void DrawBar777StopSessionMainTabRowButton(float tabBarRowScreenY, float tabBarContentY)
    {
        var session = this.bar777SessionService.GetActiveSession();
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return;
        var tabBarHeight = tabBarContentY - tabBarRowScreenY - ImGui.GetStyle().ItemSpacing.Y;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 1f));
        try
        {
            const string stopLabel = "Stop Session";
            var stopBtnSize = UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, stopLabel);
            var xRight = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X - ImGui.GetScrollX();
            var xStop = xRight - stopBtnSize.X;
            var yBtn = tabBarRowScreenY + MathF.Max(0f, (tabBarHeight - 1f - stopBtnSize.Y) * 0.5f);
            var isPaused = this.bar777SessionService.IsPaused;
            var pauseLabel = isPaused ? "Continue Session" : "Pause Session";
            var pauseIcon = isPaused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
            var pauseBtnSize = UIHelper.CalcButtonSize(pauseIcon, pauseLabel);
            DrawPauseResumeButton(isPaused, pauseLabel, new Vector2(xStop - pauseBtnSize.X - 4f, yBtn));
            DrawStopSessionButton(stopLabel, new Vector2(xStop, yBtn));
        }
        finally
        {
            ImGui.PopStyleVar();
        }
    }
    private void DrawPauseResumeButton(bool isPaused, string label, Vector2 pos)
    {
        ImGui.SetCursorScreenPos(pos);
        if (isPaused)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.04f, 0.38f, 0.08f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.08f, 0.72f, 0.15f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.06f, 0.52f, 0.10f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Border, EmporiumNeonTheme.MinefieldGreenDim);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.62f, 0.56f, 0.03f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.78f, 0.72f, 0.04f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.48f, 0.44f, 0.02f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Border, EmporiumNeonTheme.GamblerDerbyYellowDim);
        }
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.98f, 0.98f, 1f));
        try
        {
            var icon = isPaused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
            if (UIHelper.IconTextButton(icon, label, "##MGE_Bar777PauseMain"))
            {
                if (isPaused) this.bar777SessionService.ResumeSession();
                else this.bar777SessionService.PauseSession();
            }
        }
        finally
        {
            ImGui.PopStyleColor(5);
        }
    }
    private void DrawStopSessionButton(string label, Vector2 pos)
    {
        ImGui.SetCursorScreenPos(pos);
        ImGui.PushStyleColor(ImGuiCol.Button, EmporiumNeonTheme.Bar777Red);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.22f, 0.38f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.72f, 0.08f, 0.22f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Border, EmporiumNeonTheme.Bar777RedDim);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.98f, 0.98f, 1f));
        try
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, label, "##MGE_Bar777StopMain"))
                this.pendingStopConfirm = true;
        }
        finally
        {
            ImGui.PopStyleColor(5);
        }
    }
    private void DrawStopSessionConfirmPopup()
    {
        if (this.pendingStopConfirm)
        {
            ImGui.OpenPopup("##StopSessionConfirm");
            this.pendingStopConfirm = false;
        }
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.Popup("##StopSessionConfirm", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "Stop BAR 777 session?");
        ImGui.Separator();
        ImGui.Spacing();
        var stopMessage = this.config.Bar777.UseQueue
            ? "The queue will be cleared and all session stats will be reset."
            : "All session stats will be reset.";
        ImGui.TextUnformatted(stopMessage);
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Session", "##ConfirmStop"))
        {
            this.bar777SessionService.CancelSession();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##CancelStop"))
            ImGui.CloseCurrentPopup();
    }
    private void DrawDeathrollStopButton(float tabBarRowScreenY, float tabBarContentY)
    {
        if (!this.deathrollService.IsSessionActive()) return;
        var tabBarHeight = tabBarContentY - tabBarRowScreenY - ImGui.GetStyle().ItemSpacing.Y;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 1f));
        try
        {
            const string stopLabel = "Stop Tournament";
            var stopBtnSize  = UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, stopLabel);
            var yBtn         = tabBarRowScreenY + MathF.Max(0f, (tabBarHeight - 1f - stopBtnSize.Y) * 0.5f);
            var xRight       = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X - ImGui.GetScrollX();
            var isPaused     = this.bar777SessionService.IsPaused;
            var pauseLabel   = isPaused ? "Continue Tournament" : "Pause Tournament";
            var pauseIcon    = isPaused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;
            var pauseBtnSize = UIHelper.CalcButtonSize(pauseIcon, pauseLabel);
            var xStop        = xRight - stopBtnSize.X;
            var xPause       = xStop - pauseBtnSize.X - 4f;
            DrawDeathrollPauseResumeButton(isPaused, pauseLabel, pauseIcon, new Vector2(xPause, yBtn));
            DrawDeathrollStopButtonInner(stopLabel, new Vector2(xStop, yBtn));
        }
        finally
        {
            ImGui.PopStyleVar();
        }
    }

    private void DrawDeathrollPauseResumeButton(bool isPaused, string label, FontAwesomeIcon icon, Vector2 pos)
    {
        ImGui.SetCursorScreenPos(pos);
        if (isPaused)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.04f, 0.38f, 0.08f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.08f, 0.72f, 0.15f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.06f, 0.52f, 0.10f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Border,        EmporiumNeonTheme.MinefieldGreenDim);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.62f, 0.56f, 0.03f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.78f, 0.72f, 0.04f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.48f, 0.44f, 0.02f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Border,        EmporiumNeonTheme.GamblerDerbyYellowDim);
        }
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.98f, 0.98f, 1f));
        try
        {
            if (UIHelper.IconTextButton(icon, label, "##MGE_DeathrollPauseMain"))
            {
                if (isPaused) this.bar777SessionService.ResumeSession();
                else          this.bar777SessionService.PauseSession();
            }
        }
        finally
        {
            ImGui.PopStyleColor(5);
        }
    }

    private void DrawDeathrollStopButtonInner(string label, Vector2 pos)
    {
        ImGui.SetCursorScreenPos(pos);
        ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.Bar777Red);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.22f, 0.38f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.72f, 0.08f, 0.22f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Border,        EmporiumNeonTheme.Bar777RedDim);
        ImGui.PushStyleColor(ImGuiCol.Text,          new Vector4(1f, 0.98f, 0.98f, 1f));
        try
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, label, "##MGE_DeathrollStopMain"))
                this.pendingDeathrollStopConfirm = true;
        }
        finally
        {
            ImGui.PopStyleColor(5);
        }
    }
    private void DrawDeathrollStopConfirmPopup()
    {
        if (this.pendingDeathrollStopConfirm)
        {
            ImGui.OpenPopup("##DeathrollStopConfirm");
            this.pendingDeathrollStopConfirm = false;
        }
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.Popup("##DeathrollStopConfirm", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Stop Deathroll Tournament?");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("The tournament bracket and all session data will be cleared.");
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Tournament", "##ConfirmDeathrollStop"))
        {
            this.bar777SessionService.ResumeSession();
            this.deathrollService.StopSession();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##CancelDeathrollStop"))
            ImGui.CloseCurrentPopup();
    }
    private void DrawSessionHistoryTab()
    {
        using var tab = ImRaii.TabItem("Session History");
        if (tab.Success) this.sessionHistoryTab.Draw();
    }
    private void DrawTransactionHistoryTab()
    {
        using var tab = ImRaii.TabItem("Transaction History");
        if (tab.Success) this.transactionHistoryTab.Draw();
    }
    private void DrawMiniGamesTab()
    {
        using var tab = ImRaii.TabItem("Mini Games");
        this.miniGamesTabActive = tab.Success;
        if (tab.Success) this.miniGamesTab.Draw();
    }
    private void DrawPresetManagerTab()
    {
        using var tab = ImRaii.TabItem("Preset Manager");
        if (tab.Success) this.presetManagerTab.Draw();
    }
    private void DrawSettingsTab()
    {
        var flags = focusSettingsTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
        using var tab = ImRaii.TabItem("Settings", flags);
        if (tab.Success) this.settingsTab.Draw();
        focusSettingsTab = false;
    }
    private void DrawSupportTab()
    {
        using var tab = ImRaii.TabItem("Support");
        if (tab.Success) this.supportTab.Draw();
    }

    private void DrawHigherLowerStopButton(float tabBarRowScreenY, float tabBarContentY)
    {
        if (!this.higherLowerService.IsSessionActive()) return;
        var tabBarHeight = tabBarContentY - tabBarRowScreenY - ImGui.GetStyle().ItemSpacing.Y;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 1f));
        try
        {
            const string stopLabel = "Stop Session";
            var stopBtnSize = UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, stopLabel);
            var yBtn        = tabBarRowScreenY + MathF.Max(0f, (tabBarHeight - 1f - stopBtnSize.Y) * 0.5f);
            var xRight      = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X - ImGui.GetScrollX();
            var xStop       = xRight - stopBtnSize.X;
            ImGui.SetCursorScreenPos(new Vector2(xStop, yBtn));
            ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.Bar777Red);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.22f, 0.38f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.72f, 0.08f, 0.22f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Border,        EmporiumNeonTheme.Bar777RedDim);
            ImGui.PushStyleColor(ImGuiCol.Text,          new Vector4(1f, 0.98f, 0.98f, 1f));
            try
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, stopLabel, "##MGE_HLStopMain"))
                    this.pendingHLStopConfirm = true;
            }
            finally
            {
                ImGui.PopStyleColor(5);
            }
        }
        finally
        {
            ImGui.PopStyleVar();
        }
    }

    private void DrawHLStopConfirmPopup()
    {
        if (this.pendingHLStopConfirm)
        {
            ImGui.OpenPopup("##HLStopConfirm");
            this.pendingHLStopConfirm = false;
        }
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.Popup("##HLStopConfirm", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.HigherLowerOrange, "Stop Higher/Lower session?");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("All session stats will be reset.");
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Session", "##ConfirmHLStop"))
        {
            this.higherLowerService.CancelSession();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##CancelHLStop"))
            ImGui.CloseCurrentPopup();
    }
}
