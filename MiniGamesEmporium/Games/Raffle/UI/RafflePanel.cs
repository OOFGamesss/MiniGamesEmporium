using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Automation;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.Raffle.UI.Components;
using MiniGamesEmporium.Games.Raffle.UI.Tabs;
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

    private static readonly Vector4 CardAccent = EmporiumNeonTheme.RaffleTeal;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard infoCard = new();
    private const string DoorSurfaceStart    = "StartDoor";

    private float trackedStartDoorSpanPx    = GameSessionDoorStyles.RaffleStartDoor.SeedTrackedContentSpanPx;

    private readonly PluginConfiguration config;
    private readonly RaffleService service;
    private readonly RaffleGameTab gameTab;
    private readonly RaffleChatSettingsTab chatTab;
    private readonly RaffleSettingsTab settingsTab;
    private readonly RaffleChatAutomation chatAutomation;
    private readonly VenueCreditFooter venueCredit;

    public RafflePanel(
        PluginConfiguration config,
        RaffleService service,
        ChatQueueService chatQueue,
        HistoryService historyService,
        AutoPayoutService autoPayoutService,
        PlayerInfoService playerInfo)
    {
        this.config         = config;
        this.service        = service;
        this.gameTab        = new RaffleGameTab(config, service, chatQueue, historyService, autoPayoutService, playerInfo);
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

    public bool DrawSessionActionButtons()
    {
        this.gameTab.DrawSessionActionButtons();
        return true;
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

    private void DrawGameInfoCard() =>
        this.infoCard.Draw("##RaffleGameInfoCard", "Game Info", CardAccent, CardTitle, DrawGameInfoBody);

    private static void DrawGameInfoBody()
    {
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X);
        ImGui.TextDisabled("1.  Players trade Gil to buy tickets - each ticket gets the next number in the pool.");
        ImGui.TextDisabled("2.  Set a ticket cost (or 0 for free), a per-player limit and an optional closing time.");
        ImGui.TextDisabled("3.  When ready, click Draw Winner and roll /random for the highest ticket number.");
        ImGui.TextDisabled("4.  The player holding that number wins the whole pot.");
        ImGui.PopTextWrapPos();
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
        using var startColours = UIHelper.PushButtonColours(GreenButton, GreenButtonHovered, GreenButtonActive);
        var clicked = UIHelper.CentredIconTextButton(FontAwesomeIcon.Play, "Start Session", "##RaffleStartSession");
        if (clicked) this.service.StartSession();
    }
}
