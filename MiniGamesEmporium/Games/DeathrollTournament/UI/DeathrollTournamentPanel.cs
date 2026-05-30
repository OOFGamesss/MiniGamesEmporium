using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Games.Bar777.State;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.DeathrollTournament.Automation;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.UI.Components;
using MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
using MiniGamesEmporium.Services;

using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Top-level UI panel for Deathroll Tournament, rendering the pre-session start door when no session is active, and the bracket, chat settings, and settings tabs during an active tournament.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI;
public sealed class DeathrollTournamentPanel : IDisposable
{
    private static readonly Vector4 GreenButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);
    private const string DoorSurfaceStart    = "StartDoor";
    private const string DoorSurfaceBlocking = "BlockingDoor";
    private float trackedStartDoorSpanPx    = GameSessionDoorStyles.DeathrollTournamentStartDoor.SeedTrackedContentSpanPx;
    private float trackedBlockingDoorSpanPx = GameSessionDoorStyles.DeathrollTournamentBlockingDoor.SeedTrackedContentSpanPx;
    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private readonly SessionService sessionService;
    private readonly DeathrollBracketTab bracketTab;
    private readonly DeathrollChatSettingsTab chatSettingsTab;
    private readonly DeathrollSettingsTab settingsTab;
    private readonly DeathrollStatsTab statsTab;
    private readonly DeathrollDiscordWebhookTab discordTab;
    private readonly DeathrollChatAutomation chatAutomation;
    private readonly DeathrollNextMatchAutomation nextMatchAutomation;

    public DeathrollTournamentPanel(
        PluginConfiguration config,
        DeathrollTournamentService deathrollService,
        DeathrollDiscordWebhookService discordService,
        ChatQueueService chatQueue,
        SessionService sessionService,
        IPluginLog log,
        HistoryService historyService,
        AutoPayoutService autoPayoutService)
    {
        this.config              = config;
        this.deathrollService    = deathrollService;
        this.sessionService      = sessionService;
        this.bracketTab          = new DeathrollBracketTab(config, deathrollService, chatQueue, autoPayoutService);
        this.chatSettingsTab     = new DeathrollChatSettingsTab(config);
        this.settingsTab         = new DeathrollSettingsTab(config);
        this.statsTab            = new DeathrollStatsTab(config, deathrollService, chatQueue, historyService);
        this.discordTab          = new DeathrollDiscordWebhookTab(config, discordService, log);
        this.chatAutomation      = new DeathrollChatAutomation(config, deathrollService, chatQueue);
        this.nextMatchAutomation = new DeathrollNextMatchAutomation(config, deathrollService);
    }

    public void Dispose()
    {
        this.chatAutomation.Dispose();
        this.nextMatchAutomation.Dispose();
    }

    public void Draw()
    {
        if (!this.deathrollService.IsSessionActive())
        {
            DrawStartDoor();
            return;
        }
        ImGui.Spacing();
        using var chrome = new EmporiumNeonTheme.DeathrollTournamentNestedTabChromeScope();
        using var tabBar = ImRaii.TabBar("##DR_TabBar_v1");
        if (!tabBar.Success) return;
        DrawBracketTab();
        DrawChatTab();
        DrawSettingsTab();
        DrawDiscordTab();
    }

    private void DrawBracketTab()
    {
        using var tab = ImRaii.TabItem("Game");
        if (!tab.Success) return;
        ImGui.Spacing();
        var statsH          = DeathrollStatsTab.GetInlineHeight();
        var isPreTournament = !this.deathrollService.HasActiveTournament();
        this.bracketTab.Draw(
            skipLeadingSpacing: true,
            reserveBottom: statsH,
            drawStatsInline: isPreTournament ? this.statsTab.DrawInline : null);
        if (!isPreTournament)
        {
            var targetY = ImGui.GetContentRegionMax().Y - statsH;
            if (targetY > ImGui.GetCursorPosY())
                ImGui.SetCursorPosY(targetY);
            this.statsTab.DrawInline();
        }
    }

    private void DrawChatTab()
    {
        using var tab = ImRaii.TabItem("Chat");
        if (!tab.Success) return;
        using var scroll = ImRaii.Child("##DRChatScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.chatSettingsTab.Draw();
    }

    private void DrawSettingsTab()
    {
        using var tab = ImRaii.TabItem("Settings");
        if (!tab.Success) return;
        this.settingsTab.Draw();
    }

    private void DrawDiscordTab()
    {
        using var tab = ImRaii.TabItem("Discord");
        if (!tab.Success) return;
        using var scroll = ImRaii.Child("##DRDiscordScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.discordTab.Draw();
    }

    private void DrawStartDoor()
    {
        var bar777Session = this.sessionService.GetActiveSession();
        if (bar777Session != null && Bar777GameIds.Matches(bar777Session.GameName))
        {
            DrawBar777BlockingDoor(bar777Session);
            return;
        }
        DrawGameInfoCard();
        ImGui.Spacing();
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.DeathrollTournament,
            DoorSurfaceStart,
            ref this.trackedStartDoorSpanPx,
            GameSessionDoorStyles.DeathrollTournamentStartDoor,
            DrawStartDoorBody);
    }

    private void DrawBar777BlockingDoor(ActiveSessionState session)
    {
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.DeathrollTournament,
            DoorSurfaceBlocking,
            ref this.trackedBlockingDoorSpanPx,
            GameSessionDoorStyles.DeathrollTournamentBlockingDoor,
            () => DrawBar777BlockingDoorBody(session));
    }

    private void DrawBar777BlockingDoorBody(ActiveSessionState session)
    {
        var wrapEnd = ImGui.GetCursorPos().X + MathF.Max(8f, ImGui.GetContentRegionAvail().X);
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextColored(
            EmporiumNeonTheme.WarningPanel,
            $"A BAR 777 session is currently active ({session.PlayerName}). Stop it before starting a Deathroll Tournament.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Discard BAR 777 Session", "##DiscardBar777Session"))
            this.sessionService.CancelSession();
    }

    private float trackedGameInfoCardSpanPx = 80f;
    private void DrawGameInfoCard()
    {
        var containerH = MathF.Max(80f, this.trackedGameInfoCardSpanPx + 14f);
        using var card = ImRaii.Child("##DRGameInfoCard", new Vector2(-1f, containerH), true, ImGuiWindowFlags.NoScrollbar);
        if (!card.Success) return;
        var topY = ImGui.GetCursorPosY();
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Game Info");
        ImGui.Separator();
        ImGui.Spacing();
        var wrapEnd = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextDisabled("1.  Collect the entry cost from each player before the tournament begins.");
        ImGui.TextDisabled("2.  Add all entrants, optionally shuffle, then configure best-of settings per round.");
        ImGui.TextDisabled("3.  Both players in each match roll /random 10 - the highest roller goes first.");
        ImGui.TextDisabled("4.  The first player does /random. The result becomes the cap for the opponent.");
        ImGui.TextDisabled("5.  Play alternates back and forth. The first to roll a 1 loses that game.");
        ImGui.TextDisabled("6.  Winner advances through the bracket. Final winner takes the entire pot.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        this.trackedGameInfoCardSpanPx = MathF.Max(80f, ImGui.GetCursorPosY() - topY);
    }

    private void DrawStartDoorBody()
    {
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Start a Session");
        ImGui.Spacing();
        DeathrollPreSessionSettingsFields.Draw(this.config);
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
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Session", "##DRStartSession");
        ImGui.PopStyleColor(3);
        if (clicked) this.deathrollService.StartSession();
    }
}
