using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.Bar777.Actions;
using MiniGamesEmporium.Games.Bar777.Automation;
using MiniGamesEmporium.Games.Bar777.State;
using MiniGamesEmporium.Games.Bar777.UI.Components;
using MiniGamesEmporium.Games.Bar777.UI.Tabs;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Services;

using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.Utility;
using System;
using System.Numerics;

/// <summary>Top-level UI panel for BAR 777, rendering the pre-session start door or blocking door when no session is active, and the game, chat settings, and settings tabs during an active session.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI;
public sealed class Bar777Panel : IDisposable
{
    private static readonly Vector4 GreenButton = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive = new(0.10f, 0.70f, 0.28f, 1f);
    private const float GameTabQueueSectionWidthPx = 312f;
    private const string DoorSurfaceStart = "StartDoor";
    private const string DoorSurfaceBlocking = "BlockingDoor";
    private float trackedBar777StartDoorSpanPx = GameSessionDoorStyles.Bar777StartDoor.SeedTrackedContentSpanPx;
    private float trackedBar777BlockingDoorSpanPx =
        GameSessionDoorStyles.Bar777BlockingDoor.SeedTrackedContentSpanPx;
    private float trackedGameInfoCardSpanPx = 140f;
    private readonly PluginConfiguration config;
    private readonly SessionService sessionService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly Bar777GameTab gameTab;
    private readonly QueuePanel queuePanel;
    private readonly Bar777SettingsTab bar777SettingsTab;
    private readonly Bar777ChatSettingsTab bar777ChatSettingsTab;
    private readonly Bar777StatsTab bar777StatsTab;
    private readonly ChatQueueService chatQueue;
    private readonly Bar777ChatAutomation chatAutomation;
    public Bar777Panel(PluginConfiguration config, SessionService sessionService, ChatQueueService chatQueue, DeathrollTournamentService deathrollService, HistoryService historyService, AutoPayoutService autoPayoutService)
    {
        this.config = config;
        this.sessionService = sessionService;
        this.chatQueue = chatQueue;
        this.deathrollService = deathrollService;
        this.gameTab = new Bar777GameTab(config, sessionService, chatQueue, autoPayoutService);
        this.queuePanel = new QueuePanel(sessionService);
        this.bar777SettingsTab = new Bar777SettingsTab(config);
        this.bar777ChatSettingsTab = new Bar777ChatSettingsTab(config);
        this.bar777StatsTab = new Bar777StatsTab(config, chatQueue, historyService);
        this.chatAutomation = new Bar777ChatAutomation(config, sessionService, chatQueue);
    }
    public void Dispose()
    {
        this.chatAutomation.Dispose();
    }
    public void Draw()
    {
        ImGui.Spacing();
        using var bar777TabsChrome = new EmporiumNeonTheme.Bar777NestedTabChromeScope();
        using var tabBar = ImRaii.TabBar("##Bar777_TabBar_v8");
        if (!tabBar.Success) return;
        DrawGameWithQueueTab();
        DrawBar777ChatSettingsTab();
        DrawBar777SettingsTab();
    }
    private void DrawGameWithQueueTab()
    {
        using var tab = ImRaii.TabItem("Game");
        if (!tab.Success)
            return;
        var session = this.sessionService.GetActiveSession();
        if (session == null || !Bar777GameIds.Matches(session.GameName))
        {
            DrawStartSessionDoor(session);
            return;
        }
        ImGui.Spacing();
        if (!this.config.Bar777.UseQueue)
        {
            this.gameTab.Draw(skipLeadingSpacing: true);
            var statsHWalkIn = Bar777StatsTab.GetInlineHeight(showQueue: false);
            var targetYWalkIn = ImGui.GetContentRegionMax().Y - statsHWalkIn;
            if (targetYWalkIn > ImGui.GetCursorPosY())
                ImGui.SetCursorPosY(targetYWalkIn);
            this.bar777StatsTab.DrawInline(showQueue: false);
            return;
        }
        var splitHeightPx = MathF.Max(140f, ImGui.GetContentRegionAvail().Y);
        using var split = ImRaii.Table(
            "##Bar777_GameQueueSplit_v2",
            2,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1, splitHeightPx));
        if (!split.Success)
            return;
        ImGui.TableSetupColumn("##Bar777_GameCol", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##Bar777_QueueCol", ImGuiTableColumnFlags.WidthFixed, GameTabQueueSectionWidthPx);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        {
            using var gamePane = ImRaii.Child("##Bar777_GamePane", new Vector2(-1, splitHeightPx), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            if (gamePane.Success)
            {
                this.gameTab.Draw(skipLeadingSpacing: true);
                var statsH = Bar777StatsTab.GetInlineHeight(showQueue: true);
                var targetY = ImGui.GetContentRegionMax().Y - statsH;
                if (targetY > ImGui.GetCursorPosY())
                    ImGui.SetCursorPosY(targetY);
                this.bar777StatsTab.DrawInline(showQueue: true);
            }
        }
        ImGui.TableSetColumnIndex(1);
        var live = this.sessionService.GetActiveSession();
        var activeName =
            live is { PlayerName: { Length: > 0 } pn } ? pn.Trim() : null;
        string? currentForSidebar = null;
        if (!string.IsNullOrWhiteSpace(activeName) && !Bar777GameIds.IsAnyPlaceholder(activeName))
        {
            var activeWorld = live?.PlayerWorld;
            currentForSidebar = string.IsNullOrEmpty(activeWorld)
                ? activeName
                : $"{activeName}@{activeWorld}";
        }
        var showReminders = this.config.Bar777.Chat.AutoSendReminderToPlay;
        this.queuePanel.Draw(
            NearbyPlayerList.GetSorted(),
            fillColumnHeight: true,
            currentForSidebar,
            hasBeenReminded: showReminders ? this.chatAutomation.HasBeenReminded : null,
            onManualReminder: showReminders ? this.chatAutomation.SendManualReminder : null,
            onAnnounceKeyword: () => AnnounceKeyword.Execute(this.config, this.chatQueue),
            onToBackQueue: () => this.sessionService.SendCurrentBar777ToBackOfWaitlistAndStartNext(),
            onRemoveFromQueue: () => this.sessionService.RemoveCurrentBar777FromWaitlistAndStartNext(),
            isQueuePaused: this.sessionService.IsQueuePaused,
            onToggleQueuePause: () =>
            {
                if (this.sessionService.IsQueuePaused) this.sessionService.ResumeQueue();
                else this.sessionService.PauseQueue();
            });
    }
    private void DrawBar777ChatSettingsTab()
    {
        using var tab = ImRaii.TabItem("Chat");
        if (!tab.Success) return;
        using var scroll = ImRaii.Child("##Bar777ChatScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.bar777ChatSettingsTab.Draw();
    }
    private void DrawStartSessionDoor(ActiveSessionState? session)
    {
        if (this.deathrollService.IsSessionActive())
        {
            DrawDeathrollBlockingDoor();
            return;
        }
        if (session != null && !Bar777GameIds.Matches(session.GameName))
        {
            DrawBlockingSessionDoor(session);
            return;
        }
        DrawGameInfoDoorCard();
        ImGui.Spacing();
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.Bar777,
            DoorSurfaceStart,
            ref this.trackedBar777StartDoorSpanPx,
            GameSessionDoorStyles.Bar777StartDoor,
            DrawBar777DoorStartBody);
    }

    private void DrawDeathrollBlockingDoor()
    {
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.Bar777,
            DoorSurfaceBlocking,
            ref this.trackedBar777BlockingDoorSpanPx,
            GameSessionDoorStyles.Bar777BlockingDoor,
            DrawDeathrollBlockingDoorBody);
    }

    private void DrawDeathrollBlockingDoorBody()
    {
        var wrapEnd = ImGui.GetCursorPos().X + MathF.Max(8f, ImGui.GetContentRegionAvail().X);
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextColored(
            EmporiumNeonTheme.WarningPanel,
            "A Deathroll Tournament session is currently active. Stop it before opening BAR 777.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Discard Tournament Session", "##DiscardDeathrollSession"))
            this.deathrollService.StopSession();
    }
    private void DrawGameInfoDoorCard()
    {
        var containerH = MathF.Max(80f, this.trackedGameInfoCardSpanPx + 14f);
        using var card = ImRaii.Child("##Bar777GameInfoCard", new Vector2(-1f, containerH), true, ImGuiWindowFlags.NoScrollbar);
        if (!card.Success)
            return;
        var topY = ImGui.GetCursorPosY();
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "Game Info");
        ImGui.Separator();
        ImGui.Spacing();
        var wrapEnd = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextDisabled("1.  Collect the entry cost from the player before their rolls begin.");
        ImGui.TextDisabled("2.  The player types /random in chat the configured number of times (roll count).");
        ImGui.TextDisabled("3.  If they roll the winning number, they win the entire pot.");
        ImGui.TextDisabled("4.  If they lose, their bet is added to the pot for the next round.");
        ImGui.TextDisabled("5.  Use Walk-in for smaller venues, or Queue for larger venues with 10 or more players.");
        ImGui.TextDisabled("6.  Queue players join via a chat keyword and receive an alert when they are coming up next.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        this.trackedGameInfoCardSpanPx = MathF.Max(80f, ImGui.GetCursorPosY() - topY);
    }
    private void DrawBar777DoorStartBody()
    {
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "Start a Session");
        ImGui.Spacing();
        Bar777PreSessionSettingsFields.Draw(this.config);
        if (this.config.Bar777.UseQueue)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            Bar777QueueKeywordFields.Draw(this.config, "Door");
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        CentreStartSessionButton();
    }
    private void DrawBlockingSessionDoor(ActiveSessionState session)
    {
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.Bar777,
            DoorSurfaceBlocking,
            ref this.trackedBar777BlockingDoorSpanPx,
            GameSessionDoorStyles.Bar777BlockingDoor,
            () => DrawBlockingDoorBody(session));
    }
    private void DrawBlockingDoorBody(ActiveSessionState session)
    {
        var wrapEnd = ImGui.GetCursorPos().X + MathF.Max(8f, ImGui.GetContentRegionAvail().X);
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextColored(
            EmporiumNeonTheme.WarningPanel,
            $"Another game session is still saved ({FormatBlockingSessionLabel(session)}). Clear it before opening BAR 777.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Discard Saved Session", "##DiscardBlockingSession"))
        {
            this.sessionService.CancelSession();
        }
    }
    private void CentreStartSessionButton()
    {
        var queue = this.sessionService.Queue;
        string playerForSession;
        if (!this.config.Bar777.UseQueue)
            playerForSession = Bar777GameIds.WalkInPlayerPlaceholder;
        else
            playerForSession = queue.Count > 0 ? queue[0] : Bar777GameIds.WaitingPlayerPlaceholder;
        var startBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Play, "Start Session").X;
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - startBtnW) * 0.5f);
        ImGui.PushStyleColor(ImGuiCol.Button, GreenButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, GreenButtonActive);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Session", "##StartBar777Door");
        ImGui.PopStyleColor(3);
        if (clicked)
            this.sessionService.StartSession(Bar777GameIds.DisplayName, playerForSession);
    }
    private static string FormatBlockingSessionLabel(ActiveSessionState session)
    {
        var game = string.IsNullOrWhiteSpace(session.GameName) ? "unknown game" : session.GameName.Trim();
        var p = string.IsNullOrWhiteSpace(session.PlayerName) ? "unknown player" : session.PlayerName.Trim();
        return $"{game} / {p}";
    }
    private void DrawBar777SettingsTab()
    {
        using var tab = ImRaii.TabItem("Settings");
        if (!tab.Success) return;
        this.bar777SettingsTab.Draw();
    }
}
