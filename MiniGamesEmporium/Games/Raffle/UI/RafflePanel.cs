using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Automation;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.Raffle.UI.Components;
using MiniGamesEmporium.Games.Raffle.UI.Tabs;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>Top-level UI panel for the Raffle game.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI;
public sealed class RafflePanel : IDisposable
{
    private static readonly Vector4 GreenButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);
    private const string DoorSurfaceStart    = "StartDoor";
    private const string DoorSurfaceBlocking = "BlockingDoor";

    private float trackedStartDoorSpanPx    = GameSessionDoorStyles.RaffleStartDoor.SeedTrackedContentSpanPx;
    private float trackedBlockingDoorSpanPx = GameSessionDoorStyles.RaffleBlockingDoor.SeedTrackedContentSpanPx;
    private float trackedGameInfoCardSpanPx = 96f;

    private readonly PluginConfiguration config;
    private readonly RaffleService service;
    private readonly SessionService sessionService;
    private readonly RaffleGameTab gameTab;
    private readonly RaffleChatSettingsTab chatTab;
    private readonly RaffleSettingsTab settingsTab;
    private readonly RaffleChatAutomation chatAutomation;
    private readonly VenueCreditFooter venueCredit;

    public RafflePanel(
        PluginConfiguration config,
        RaffleService service,
        ChatQueueService chatQueue,
        SessionService sessionService,
        HistoryService historyService,
        AutoPayoutService autoPayoutService)
    {
        this.config         = config;
        this.service        = service;
        this.sessionService = sessionService;
        this.gameTab        = new RaffleGameTab(config, service, chatQueue, historyService, autoPayoutService);
        this.chatTab        = new RaffleChatSettingsTab(config);
        this.settingsTab    = new RaffleSettingsTab(config);
        this.chatAutomation = new RaffleChatAutomation(config, service, chatQueue);
        this.venueCredit    = new VenueCreditFooter("habitat.png", "Habitat");
    }

    public void Dispose()
    {
        this.chatAutomation.Dispose();
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
        if (!this.service.IsSessionActive())
        {
            DrawStartDoor();
            return;
        }
        this.gameTab.Draw();
    }

    private void DrawChatSection()
    {
        using var scroll = ImRaii.Child("##RaffleChatScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.chatTab.Draw();
    }

    private void DrawSettingsSection()
    {
        this.settingsTab.Draw();
    }

    private void DrawStartDoor()
    {
        var blocking = this.sessionService.GetBlockingGameName(RaffleGameIds.DisplayName);
        if (blocking != null)
        {
            GameSessionDoorHost.Draw(
                KnownGameDoorModules.Raffle,
                DoorSurfaceBlocking,
                ref this.trackedBlockingDoorSpanPx,
                GameSessionDoorStyles.RaffleBlockingDoor,
                () => DrawBlockingDoorBody(blocking));
            return;
        }
        DrawGameInfoCard();
        ImGui.Spacing();
        var doorAreaH = MathF.Max(120f, ImGui.GetContentRegionAvail().Y - VenueCreditFooter.RowHeight());
        using (var doorArea = ImRaii.Child(
                   "##RaffleDoorArea",
                   new Vector2(-1f, doorAreaH),
                   false,
                   ImGuiWindowFlags.NoScrollbar))
        {
            if (doorArea.Success)
                GameSessionDoorHost.Draw(
                    KnownGameDoorModules.Raffle,
                    DoorSurfaceStart,
                    ref this.trackedStartDoorSpanPx,
                    GameSessionDoorStyles.RaffleStartDoor,
                    DrawStartDoorBody);
        }
        this.venueCredit.Draw();
    }

    private void DrawBlockingDoorBody(string blockingGameName)
    {
        var wrapEnd = ImGui.GetCursorPos().X + MathF.Max(8f, ImGui.GetContentRegionAvail().X);
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextColored(EmporiumNeonTheme.WarningPanel,
            $"{blockingGameName} is currently running. Please end or discard that game to run a Raffle.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Discard Session", "##RaffleDiscardSession"))
            this.sessionService.CancelActiveGame();
    }

    private void DrawGameInfoCard()
    {
        var containerH = MathF.Max(96f, this.trackedGameInfoCardSpanPx + 14f);
        using var card = ImRaii.Child("##RaffleGameInfoCard", new Vector2(-1f, containerH), true, ImGuiWindowFlags.NoScrollbar);
        if (!card.Success) return;
        var topY = ImGui.GetCursorPosY();
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.RaffleTeal, "Game Info");
        ImGui.Separator();
        ImGui.Spacing();
        var wrapEnd = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextDisabled("1.  Players trade Gil to buy tickets - each ticket gets the next number in the pool.");
        ImGui.TextDisabled("2.  Set a ticket cost (or 0 for free), a per-player limit and an optional closing time.");
        ImGui.TextDisabled("3.  When ready, click Draw Winner and roll /random for the highest ticket number.");
        ImGui.TextDisabled("4.  The player holding that number wins the whole pot.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        this.trackedGameInfoCardSpanPx = MathF.Max(96f, ImGui.GetCursorPosY() - topY);
    }

    private void DrawStartDoorBody()
    {
        UIHelper.DrawStartSessionHeading(EmporiumNeonTheme.RaffleTeal);
        ImGui.Spacing();
        RafflePreSessionSettingsFields.Draw(this.config);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawStartButton();
    }

    private void DrawStartButton()
    {
        var btnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Play, "Start Session").X;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - btnW) * 0.5f);
        ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Session", "##RaffleStartSession");
        ImGui.PopStyleColor(3);
        if (clicked) this.service.StartSession();
    }
}
