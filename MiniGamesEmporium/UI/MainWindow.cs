using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.UI.Tabs;
using System;
using System.Numerics;
using MiniGamesEmporium.Games.Bar777;

/// <summary>The main plugin window, hosting the top-level tab bar for Mini Games, Session History, Transaction History, and Settings, plus a floating Stop Session button overlaid on the tab band during active BAR 777 sessions.</summary>

namespace MiniGamesEmporium.UI;
public sealed class MainWindow : Window, IDisposable
{
    public event Action? WindowOpened;

    private readonly TransactionHistoryTab transactionHistoryTab;
    private readonly SessionHistoryTab sessionHistoryTab;
    private readonly MiniGamesTab miniGamesTab;
    private readonly SettingsTab settingsTab;
    private readonly SessionService sessionService;
    private readonly PluginConfiguration config;
    private bool focusSettingsTab;
    private bool pendingStopConfirm;

    public override void OnOpen() => WindowOpened?.Invoke();

    public void OpenSettingsTab()
    {
        IsOpen = true;
        focusSettingsTab = true;
    }
    public MainWindow(
        PluginConfiguration config,
        SessionService sessionService,
        ChatQueueService chatQueue)
        : base("Mini Games Emporium##MGE_Main_v2")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 440),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        this.transactionHistoryTab = new TransactionHistoryTab(config);
        this.sessionHistoryTab = new SessionHistoryTab(config);
        this.miniGamesTab = new MiniGamesTab(config, sessionService, chatQueue);
        this.settingsTab = new SettingsTab(config);
        this.sessionService = sessionService;
        this.config = config;
    }
    public void Dispose()
    {
        this.miniGamesTab.Dispose();
    }
    public override void Draw()
    {
        using var theme = new EmporiumNeonTheme.Scope();
        using var mainTabChrome = new EmporiumNeonTheme.MainWindowTabChromeScope();
        float tabBarRowScreenY = 0f;
        {
            using var tabBar = ImRaii.TabBar("##MGE_TabBar_v4");
            if (!tabBar.Success)
                return;
            tabBarRowScreenY = ImGui.GetCursorScreenPos().Y;
            DrawMiniGamesTab();
            DrawSessionHistoryTab();
            DrawTransactionHistoryTab();
            DrawSettingsTab();
        }
        DrawBar777StopSessionMainTabRowButton(tabBarRowScreenY);
        DrawStopSessionConfirmPopup();
    }
    private void DrawBar777StopSessionMainTabRowButton(float tabBarRowScreenY)
    {
        var session = this.sessionService.GetActiveSession();
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 3f));
        try
        {
            var fp = ImGui.GetStyle().FramePadding;
            var yBtn = tabBarRowScreenY + MathF.Max(0f, (ImGui.GetFrameHeight() - UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, "Stop Session").Y) * 0.5f);
            var xRight = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X - ImGui.GetScrollX();
            const string stopLabel = "Stop Session";
            var stopBtnSize = UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, stopLabel);
            var xStop = xRight - stopBtnSize.X;
            var isPaused = this.sessionService.IsPaused;
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
                if (isPaused) this.sessionService.ResumeSession();
                else this.sessionService.PauseSession();
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
            this.sessionService.CancelSession();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##CancelStop"))
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
        if (tab.Success) this.miniGamesTab.Draw();
    }
    private void DrawSettingsTab()
    {
        var flags = focusSettingsTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None;
        using var tab = ImRaii.TabItem("Settings", flags);
        if (tab.Success) this.settingsTab.Draw();
        focusSettingsTab = false;
    }
}
