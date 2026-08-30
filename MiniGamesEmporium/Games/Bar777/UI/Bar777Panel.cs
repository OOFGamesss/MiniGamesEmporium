using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.Bar777.Actions;
using MiniGamesEmporium.Games.Bar777.Automation;
using MiniGamesEmporium.Games.Bar777.UI.Components;
using MiniGamesEmporium.Games.Bar777.UI.Tabs;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>Top-level UI panel for BAR 777.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI;
public sealed class Bar777Panel : IDisposable
{
    private static readonly Vector4 GreenButton = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive = new(0.10f, 0.70f, 0.28f, 1f);

    private static readonly Vector4 CardAccent = EmporiumNeonTheme.Bar777Red;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard infoCard = new();
    private const float GameTabQueueSectionWidthPx = 312f;
    private const string DoorSurfaceStart = "StartDoor";
    private float trackedBar777StartDoorSpanPx = GameSessionDoorStyles.Bar777StartDoor.SeedTrackedContentSpanPx;
    private readonly PluginConfiguration config;
    private readonly Bar777SessionService bar777SessionService;
    private readonly Bar777GameTab gameTab;
    private readonly QueuePanel queuePanel;
    private readonly Bar777SettingsTab bar777SettingsTab;
    private readonly Bar777ChatSettingsTab bar777ChatSettingsTab;
    private readonly Bar777StatsTab bar777StatsTab;
    private readonly ChatQueueService chatQueue;
    private readonly Bar777ChatAutomation chatAutomation;
    public Bar777Panel(PluginConfiguration config, Bar777SessionService bar777SessionService, ChatQueueService chatQueue, HistoryService historyService, AutoPayoutService autoPayoutService)
    {
        this.config              = config;
        this.bar777SessionService = bar777SessionService;
        this.chatQueue           = chatQueue;
        this.gameTab        = new Bar777GameTab(config, bar777SessionService, chatQueue, autoPayoutService);
        this.queuePanel     = new QueuePanel(bar777SessionService);
        this.bar777SettingsTab     = new Bar777SettingsTab(config);
        this.bar777ChatSettingsTab = new Bar777ChatSettingsTab(config);
        this.bar777StatsTab        = new Bar777StatsTab(config, chatQueue, historyService);
        this.chatAutomation        = new Bar777ChatAutomation(config, bar777SessionService, chatQueue);
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
        var session = this.bar777SessionService.GetActiveSession();
        if (session == null || !Bar777GameIds.Matches(session.GameName))
        {
            DrawStartSessionDoor();
            return;
        }
        if (!this.config.Bar777.UseQueue)
        {
            using var gamePane = ImRaii.Child("##Bar777_GamePane", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            if (gamePane.Success)
            {
                this.gameTab.Draw(skipLeadingSpacing: true);
                var statsHWalkIn = Bar777StatsTab.GetInlineHeight(showQueue: false, showKept: this.config.Bar777.TradesToPotPercent < 100);
                var targetYWalkIn = ImGui.GetContentRegionMax().Y - statsHWalkIn;
                if (targetYWalkIn > ImGui.GetCursorPosY())
                    ImGui.SetCursorPosY(targetYWalkIn);
                this.bar777StatsTab.DrawInline(showQueue: false);
            }
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
            var cellH = ImGui.GetContentRegionAvail().Y;
            using var gamePane = ImRaii.Child("##Bar777_GamePane", new Vector2(-1, cellH), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            if (gamePane.Success)
            {
                this.gameTab.Draw(skipLeadingSpacing: true);
                var statsH = Bar777StatsTab.GetInlineHeight(showQueue: true, showKept: this.config.Bar777.TradesToPotPercent < 100);
                var targetY = ImGui.GetContentRegionMax().Y - statsH;
                if (targetY > ImGui.GetCursorPosY())
                    ImGui.SetCursorPosY(targetY);
                this.bar777StatsTab.DrawInline(showQueue: true);
            }
        }
        ImGui.TableSetColumnIndex(1);
        var live = this.bar777SessionService.GetActiveSession();
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
            PlayerInfoService.GetNearbySorted(),
            fillColumnHeight: true,
            currentForSidebar,
            hasBeenReminded: showReminders ? this.chatAutomation.HasBeenReminded : null,
            onManualReminder: showReminders ? this.chatAutomation.SendManualReminder : null,
            onAnnounceKeyword: () => AnnounceKeyword.Execute(this.config, this.chatQueue),
            onToBackQueue: () => this.bar777SessionService.SendCurrentBar777ToBackOfWaitlistAndStartNext(),
            onRemoveFromQueue: () => this.bar777SessionService.RemoveCurrentBar777FromWaitlistAndStartNext(),
            isQueuePaused: this.bar777SessionService.IsQueuePaused,
            onToggleQueuePause: () =>
            {
                if (this.bar777SessionService.IsQueuePaused) this.bar777SessionService.ResumeQueue();
                else this.bar777SessionService.PauseQueue();
            },
            onNextPlayerUp: currentForSidebar == null
                ? null
                : () => AnnounceNextPlayerUp.Execute(currentForSidebar, this.config, this.chatQueue));
    }
    private void DrawChatSection()
    {
        using var scroll = ImRaii.Child("##Bar777ChatScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.bar777ChatSettingsTab.Draw();
    }
    private void DrawStartSessionDoor()
    {
        DrawGameInfoDoorCard();
        ImGui.Spacing();
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.Bar777,
            DoorSurfaceStart,
            ref this.trackedBar777StartDoorSpanPx,
            GameSessionDoorStyles.Bar777StartDoor,
            DrawBar777DoorStartBody);
    }

    private void DrawGameInfoDoorCard() =>
        this.infoCard.Draw("##Bar777GameInfoCard", "Game Info", CardAccent, CardTitle, DrawGameInfoBody);

    private static void DrawGameInfoBody()
    {
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X);
        ImGui.TextDisabled("1.  Collect the entry cost from the player before their rolls begin.");
        ImGui.TextDisabled("2.  The player types /random in chat the configured number of times (roll count).");
        ImGui.TextDisabled("3.  If they roll the winning number, they win the entire pot.");
        ImGui.TextDisabled("4.  If they lose, their bet is added to the pot for the next round.");
        ImGui.TextDisabled("5.  Use Walk-in for smaller venues, or Queue for larger venues with 10 or more players.");
        ImGui.TextDisabled("6.  Queue players join via a chat keyword and receive an alert when they are coming up next.");
        ImGui.PopTextWrapPos();
    }
    private void DrawBar777DoorStartBody()
    {
        UIHelper.DrawStartSessionHeading(EmporiumNeonTheme.Bar777Red);
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
    private void CentreStartSessionButton()
    {
        var queue = this.bar777SessionService.Queue;
        string playerForSession;
        if (!this.config.Bar777.UseQueue)
            playerForSession = Bar777GameIds.WalkInPlayerPlaceholder;
        else
            playerForSession = queue.Count > 0 ? queue[0] : Bar777GameIds.WaitingPlayerPlaceholder;
        using var startColours = UIHelper.PushButtonColours(GreenButton, GreenButtonHovered, GreenButtonActive);
        var clicked = UIHelper.CentredIconTextButton(FontAwesomeIcon.Play, "Start Session", "##StartBar777Door");
        if (clicked)
            this.bar777SessionService.StartSession(Bar777GameIds.DisplayName, playerForSession);
    }
    private void DrawSettingsSection()
    {
        this.bar777SettingsTab.Draw();
    }
}
