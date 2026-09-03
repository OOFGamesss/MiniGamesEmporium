using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Automation;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Games.CoinCollector.UI.Components;
using MiniGamesEmporium.Games.CoinCollector.UI.Tabs;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;

/// <summary>Top-level UI panel for Coin Collector.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.UI;
public sealed class CoinCollectorPanel : IDisposable
{
    private const string DoorSurfaceStart = "StartDoor";

    private static readonly Vector4 GreenButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);

    private float trackedStartDoorSpanPx = GameSessionDoorStyles.CoinCollectorStartDoor.SeedTrackedContentSpanPx;

    private static readonly Vector4 CardAccent = EmporiumNeonTheme.CoinCollectorIndigo;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();
    private readonly PluginConfiguration config;
    private readonly CoinCollectorService coinCollectorService;
    private readonly CoinCollectorGameTab gameTab;
    private readonly CoinCollectorSettingsTab settingsTab;
    private readonly CoinCollectorChatSettingsTab chatSettingsTab;
    private readonly CoinCollectorLeaderboardTab leaderboardTab;
    private readonly CoinCollectorChatAutomation chatAutomation;
    private readonly CoinCollectorTurnAutomation turnAutomation;
    private readonly CoinCollectorQueueService queueService;

    public CoinCollectorPanel(PluginConfiguration config, CoinCollectorService coinCollectorService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService, PlayerInfoService playerInfoService, HistoryService historyService)
    {
        this.config               = config;
        this.coinCollectorService = coinCollectorService;
        this.queueService         = new CoinCollectorQueueService(config, coinCollectorService, playerInfoService);
        this.gameTab              = new CoinCollectorGameTab(config, coinCollectorService, chatQueue, autoPayoutService, this.queueService);
        this.settingsTab          = new CoinCollectorSettingsTab(config);
        this.chatSettingsTab      = new CoinCollectorChatSettingsTab(config);
        this.leaderboardTab       = new CoinCollectorLeaderboardTab(config, coinCollectorService, chatQueue, historyService);
        this.chatAutomation       = new CoinCollectorChatAutomation(config, coinCollectorService, chatQueue);
        this.turnAutomation       = new CoinCollectorTurnAutomation(config, coinCollectorService, chatQueue);
    }

    public static IReadOnlyList<GameSection> Sections { get; } =
        [GameSection.Game, GameSection.Chat, GameSection.Settings];

    public void DrawSection(GameSection section)
    {
        ImGui.Spacing();
        switch (section)
        {
            case GameSection.Game:     DrawGameSection(); break;
            case GameSection.Chat:     DrawChatSection(); break;
            case GameSection.Settings: DrawSettingsSection(); break;
        }
    }

    public bool DrawSessionActionButtons()
    {
        this.gameTab.DrawSessionActionButtons();
        return true;
    }

    private void DrawGameSection()
    {
        var session = this.coinCollectorService.GetActiveSession();
        if (session == null)
        {
            DrawStartSessionDoor();
            return;
        }

        this.queueService.Refresh();

        var statsH  = CoinCollectorLeaderboardTab.GetInlineHeight(showKept: this.config.CoinCollector.TradesToPotPercent < 100);
        var reserve = CollapsiblePanels.StatsReserveHeight(PanelKeys.CoinCollectorStats, statsH);
        this.gameTab.Draw(skipLeadingSpacing: true, reserveBottom: reserve, drawBottomPanel: DrawStatsStrip);
    }

    private void DrawStatsStrip() =>
        CollapsiblePanels.DrawStatsStrip(PanelKeys.CoinCollectorStats, "##CCStatsTag",
            CardAccent, "the stats panel", this.leaderboardTab.DrawInline);

    private void DrawChatSection()
    {
        using var scroll = ImRaii.Child("##CCChatScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.chatSettingsTab.Draw();
    }

    private void DrawSettingsSection()
    {
        using var scroll = ImRaii.Child("##CCSettingsScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.settingsTab.Draw();
    }

    private void DrawStartSessionDoor()
    {
        DrawGameInfoCard();
        ImGui.Spacing();
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.CoinCollector,
            DoorSurfaceStart,
            ref this.trackedStartDoorSpanPx,
            GameSessionDoorStyles.CoinCollectorStartDoor,
            DrawStartDoorBody);
    }

    private void DrawGameInfoCard() =>
        this.card.Draw("##CCGameInfoCard", "Game Info", CardAccent, CardTitle, DrawGameInfoBody);

    private static void DrawGameInfoBody()
    {
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X);
        ImGui.TextDisabled("1.  Invite the player to your party so everyone can see the dice rolls.");
        ImGui.TextDisabled("2.  Select the player from the list of invited players.");
        ImGui.TextDisabled("3.  Collect the entry cost from the player before their turn begins.");
        ImGui.TextDisabled("4.  The player rolls /dice to get their starting number.");
        ImGui.TextDisabled("5.  The player rolls /dice again using their last result as the new maximum.");
        ImGui.TextDisabled("6.  Every roll that is not a 1 earns a coin. Rolling a 1 ends the turn.");
        ImGui.TextDisabled("7.  Click Finish Game and declare the winner.");
        ImGui.PopTextWrapPos();
    }

    private void DrawStartDoorBody()
    {
        UIHelper.DrawStartSessionHeading(EmporiumNeonTheme.CoinCollectorIndigo);
        ImGui.Spacing();
        CoinCollectorPreSessionSettingsFields.Draw(this.config);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawCentredStartButton();
    }

    private void DrawCentredStartButton()
    {
        using var green = UIHelper.PushButtonColours(GreenButton, GreenButtonHovered, GreenButtonActive);
        if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Play, "Start Session", "##CCStartSessionDoor"))
            this.coinCollectorService.StartSession();
    }

    public void Dispose()
    {
        this.turnAutomation.Dispose();
        this.chatAutomation.Dispose();
        this.gameTab.Dispose();
        this.queueService.Dispose();
    }
}
