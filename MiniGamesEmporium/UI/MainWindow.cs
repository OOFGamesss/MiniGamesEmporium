using Dalamud.Bindings.ImGui;
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
    private readonly TransactionHistoryTab transactionHistoryTab;
    private readonly SessionHistoryTab sessionHistoryTab;
    private readonly MiniGamesTab miniGamesTab;
    private readonly SettingsTab settingsTab;
    private readonly SessionService sessionService;
    private bool focusSettingsTab;
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
    }
    private void DrawBar777StopSessionMainTabRowButton(float tabBarRowScreenY)
    {
        var session = this.sessionService.GetActiveSession();
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return;
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 3f));
        const string labelVis = "Stop Session";
        var fp = ImGui.GetStyle().FramePadding;
        var fb = ImGui.GetStyle().FrameBorderSize;
        var btnW = ImGui.CalcTextSize(labelVis).X + fp.X * 2f + fb * 2f;
        var btnH = ImGui.GetTextLineHeight() + fp.Y * 2f + fb * 2f;
        var tabBandH = ImGui.GetFrameHeight();
        var yBtn = tabBarRowScreenY + MathF.Max(0f, (tabBandH - btnH) * 0.5f);
        var winPos = ImGui.GetWindowPos();
        var crMax = ImGui.GetWindowContentRegionMax();
        var scrollX = ImGui.GetScrollX();
        var xBtn = winPos.X + crMax.X - btnW - scrollX;
        ImGui.SetCursorScreenPos(new Vector2(xBtn, yBtn));
        ImGui.PushStyleColor(ImGuiCol.Button, EmporiumNeonTheme.Bar777Red);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.22f, 0.38f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.72f, 0.08f, 0.22f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Border, EmporiumNeonTheme.Bar777RedDim);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.98f, 0.98f, 1f));
        try
        {
            if (ImGui.Button($"{labelVis}##MGE_Bar777StopMain", new Vector2(btnW, btnH)))
                this.sessionService.CancelSession();
        }
        finally
        {
            ImGui.PopStyleColor(5);
            ImGui.PopStyleVar();
        }
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
