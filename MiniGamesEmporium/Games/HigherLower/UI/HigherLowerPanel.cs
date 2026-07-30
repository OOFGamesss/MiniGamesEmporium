using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Automation;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.HigherLower.UI.Components;
using MiniGamesEmporium.Games.HigherLower.UI.Tabs;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;

/// <summary>Top-level UI panel for Higher/Lower.</summary>

namespace MiniGamesEmporium.Games.HigherLower.UI;
public sealed class HigherLowerPanel : IDisposable
{
    private const string DoorSurfaceStart    = "StartDoor";
    private const string DoorSurfaceBlocking = "BlockingDoor";

    private static readonly Vector4 GreenButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);

    private float trackedStartDoorSpanPx    = GameSessionDoorStyles.HigherLowerStartDoor.SeedTrackedContentSpanPx;
    private float trackedBlockingDoorSpanPx = GameSessionDoorStyles.HigherLowerBlockingDoor.SeedTrackedContentSpanPx;
    private float trackedGameInfoCardSpanPx = 140f;

    private readonly PluginConfiguration config;
    private readonly HigherLowerService higherLowerService;
    private readonly ChatQueueService chatQueue;
    private readonly SessionService sessionService;
    private readonly HigherLowerGameTab gameTab;
    private readonly HigherLowerSettingsTab settingsTab;
    private readonly HigherLowerChatSettingsTab chatSettingsTab;
    private readonly HigherLowerLeaderboardTab leaderboardTab;
    private readonly HigherLowerChatAutomation chatAutomation;

    public HigherLowerPanel(PluginConfiguration config, HigherLowerService higherLowerService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService, SessionService sessionService, PlayerInfoService playerInfoService, HistoryService historyService)
    {
        this.config             = config;
        this.higherLowerService = higherLowerService;
        this.chatQueue          = chatQueue;
        this.sessionService     = sessionService;
        this.gameTab            = new HigherLowerGameTab(config, higherLowerService, chatQueue, autoPayoutService, playerInfoService);
        this.settingsTab        = new HigherLowerSettingsTab(config);
        this.chatSettingsTab    = new HigherLowerChatSettingsTab(config);
        this.leaderboardTab     = new HigherLowerLeaderboardTab(config, higherLowerService, chatQueue, historyService);
        this.chatAutomation     = new HigherLowerChatAutomation(config, higherLowerService, chatQueue);
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

    private void DrawGameSection()
    {
        var session = this.higherLowerService.GetActiveSession();
        if (session == null)
        {
            DrawStartSessionDoor();
            return;
        }

        var statsH = HigherLowerLeaderboardTab.GetInlineHeight(showKept: this.config.HigherLower.TradesToPotPercent < 100);
        this.gameTab.Draw(skipLeadingSpacing: true, reserveBottom: statsH, drawBottomPanel: this.leaderboardTab.DrawInline);
    }

    private void DrawChatSection()
    {
        using var scroll = ImRaii.Child("##HLChatScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.chatSettingsTab.Draw();
    }

    private void DrawSettingsSection()
    {
        this.settingsTab.Draw();
    }

    private void DrawStartSessionDoor()
    {
        var blocking = this.sessionService.GetBlockingGameName(HigherLowerGameIds.DisplayName);
        if (blocking != null)
        {
            DrawActiveBlockingDoor(blocking);
            return;
        }
        DrawGameInfoCard();
        ImGui.Spacing();
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.HigherLower,
            DoorSurfaceStart,
            ref this.trackedStartDoorSpanPx,
            GameSessionDoorStyles.HigherLowerStartDoor,
            DrawStartDoorBody);
    }

    private void DrawGameInfoCard()
    {
        var containerH = MathF.Max(80f, this.trackedGameInfoCardSpanPx + 14f);
        using var card = ImRaii.Child("##HLGameInfoCard", new Vector2(-1f, containerH), true, ImGuiWindowFlags.NoScrollbar);
        if (!card.Success) return;
        var topY = ImGui.GetCursorPosY();
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.HigherLowerOrange, "Game Info");
        ImGui.Separator();
        ImGui.Spacing();
        var wrapEnd = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextDisabled("1.  Invite the player to your party so they can see the dice rolls.");
        ImGui.TextDisabled("2.  Select the player from the list of invited players.");
        ImGui.TextDisabled("3.  Collect the entry cost from the player before their turn begins.");
        ImGui.TextDisabled("4.  Roll /dice X (configured dice sides) to get the opening number.");
        ImGui.TextDisabled("5.  The player guesses Higher or Lower for the next roll.");
        ImGui.TextDisabled("6.  Roll again - if correct the round count increases, if wrong the game ends.");
        ImGui.TextDisabled("7.  Click Finish Game and declare the winner.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        this.trackedGameInfoCardSpanPx = MathF.Max(80f, ImGui.GetCursorPosY() - topY);
    }

    private void DrawStartDoorBody()
    {
        UIHelper.DrawStartSessionHeading(EmporiumNeonTheme.HigherLowerOrange);
        ImGui.Spacing();
        HigherLowerPreSessionSettingsFields.Draw(this.config);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawCentredStartButton();
    }

    private void DrawActiveBlockingDoor(string blockingGameName)
    {
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.HigherLower,
            DoorSurfaceBlocking,
            ref this.trackedBlockingDoorSpanPx,
            GameSessionDoorStyles.HigherLowerBlockingDoor,
            () => DrawActiveBlockingDoorBody(blockingGameName));
    }

    private void DrawActiveBlockingDoorBody(string blockingGameName)
    {
        var wrapEnd = ImGui.GetCursorPos().X + MathF.Max(8f, ImGui.GetContentRegionAvail().X);
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextColored(EmporiumNeonTheme.WarningPanel,
            $"{blockingGameName} is currently running. Please end or discard the game to play {HigherLowerGameIds.DisplayName}.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Discard Session", "##HLDiscardSession"))
            this.sessionService.CancelActiveGame();
    }

    private void DrawCentredStartButton()
    {
        var startBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Play, "Start Session").X;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - startBtnW) * 0.5f);
        ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Session", "##HLStartSessionDoor");
        ImGui.PopStyleColor(3);
        if (clicked)
            this.higherLowerService.StartSession();
    }

    public void Dispose()
    {
        this.chatAutomation.Dispose();
    }
}
