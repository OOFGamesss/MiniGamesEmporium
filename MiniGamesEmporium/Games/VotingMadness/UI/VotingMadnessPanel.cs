using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Games.VotingMadness.UI.Components;
using MiniGamesEmporium.Games.VotingMadness.UI.Tabs;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;

/// <summary>Top-level UI panel for Voting Madness.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.UI;
public sealed class VotingMadnessPanel
{
    private static readonly Vector4 GreenButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);

    private static readonly Vector4 CardAccent = EmporiumNeonTheme.VotingMadnessLime;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard infoCard = new();

    private const string DoorSurfaceStart    = "StartDoor";

    private float trackedStartDoorSpanPx    = GameSessionDoorStyles.VotingMadnessStartDoor.SeedTrackedContentSpanPx;

    private readonly PluginConfiguration config;
    private readonly VotingMadnessService service;
    private readonly VotingMadnessGameTab gameTab;
    private readonly VotingMadnessChatSettingsTab chatTab;
    private readonly VotingMadnessSettingsTab settingsTab;
    private readonly VenueCreditFooter venueCredit;

    public VotingMadnessPanel(
        PluginConfiguration config,
        VotingMadnessService service,
        ChatQueueService chatQueue)
    {
        this.config         = config;
        this.service        = service;
        this.gameTab        = new VotingMadnessGameTab(config, service, chatQueue);
        this.chatTab        = new VotingMadnessChatSettingsTab(config);
        this.settingsTab    = new VotingMadnessSettingsTab(config);
        this.venueCredit    = new VenueCreditFooter("1up.png", "1-UP");
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
        using var scroll = ImRaii.Child("##VMChatScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.chatTab.Draw();
    }

    private void DrawSettingsSection() => this.settingsTab.Draw();

    private void DrawStartDoor()
    {
        DrawGameInfoCard();
        ImGui.Spacing();
        var doorAreaH = MathF.Max(120f, ImGui.GetContentRegionAvail().Y - VenueCreditFooter.RowHeight());
        using (var doorArea = ImRaii.Child(
                   "##VMDoorArea",
                   new Vector2(-1f, doorAreaH),
                   false,
                   ImGuiWindowFlags.NoScrollbar))
        {
            if (doorArea.Success)
                GameSessionDoorHost.Draw(
                    KnownGameDoorModules.VotingMadness,
                    DoorSurfaceStart,
                    ref this.trackedStartDoorSpanPx,
                    GameSessionDoorStyles.VotingMadnessStartDoor,
                    DrawStartDoorBody);
        }
        this.venueCredit.Draw();
    }

    private void DrawGameInfoCard() =>
        this.infoCard.Draw("##VMGameInfoCard", "Game Info", CardAccent, CardTitle, DrawGameInfoBody);

    private static void DrawGameInfoBody()
    {
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X);
        ImGui.TextDisabled("1.  Add at least two voting keywords and choose which chats to listen on.");
        ImGui.TextDisabled("2.  Decide whether players may pick multiple options and whether they may vote more than once.");
        ImGui.TextDisabled("3.  Start the session and players cast votes by saying a keyword in chat (host messages are ignored).");
        ImGui.TextDisabled("4.  Watch the live bar chart and voter table, then stop the vote and announce the winner.");
        ImGui.PopTextWrapPos();
    }

    private void DrawStartDoorBody()
    {
        UIHelper.DrawStartSessionHeading(EmporiumNeonTheme.VotingMadnessLime);
        ImGui.Spacing();
        VotingMadnessPreSessionSettingsFields.Draw(this.config);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawStartButton();
    }

    private void DrawStartButton()
    {
        var canStart = VotingMadnessPreSessionSettingsFields.CanStart(this.config);
        using (ImRaii.Disabled(!canStart))
        using (UIHelper.PushButtonColours(GreenButton, GreenButtonHovered, GreenButtonActive))
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Play, "Start Session", "##VMStartSessionDoor"))
                this.service.StartSession();
        }
        var duplicateTooltip = VotingMadnessPreSessionSettingsFields.GetDuplicateOptionsTooltip(this.config);
        if (duplicateTooltip != null && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(duplicateTooltip);
        if (!canStart)
        {
            var reason = VotingMadnessPreSessionSettingsFields.GetStartBlockReason(this.config);
            if (reason != null)
            {
                ImGui.Spacing();
                ImGui.TextDisabled(reason);
            }
        }
    }
}
