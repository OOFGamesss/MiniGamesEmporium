using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Actions;
using MiniGamesEmporium.Games.DeathrollTournament.Automation;
using MiniGamesEmporium.Games.DeathrollTournament.Discord;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.UI.Components;
using MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
using MiniGamesEmporium.Games.DeathrollTournament.Webview;
using MiniGamesEmporium.Services;

using MiniGamesEmporium.UI.Components;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>Top-level UI panel for Deathroll Tournament.</summary>

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
    private readonly DrtWebviewService webviewService;
    private readonly ChatQueueService chatQueue;
    private readonly DeathrollBracketTab bracketTab;
    private readonly DeathrollChatSettingsTab chatSettingsTab;
    private readonly DeathrollSettingsTab settingsTab;
    private readonly DeathrollStatsTab statsTab;
    private readonly DeathrollBetsTab betsTab;
    private readonly DeathrollDiscordWebhookTab discordTab;
    private readonly DeathrollWebviewTab webviewTab;
    private readonly DeathrollChatAutomation chatAutomation;
    private readonly DeathrollNextMatchAutomation nextMatchAutomation;

    public DeathrollTournamentPanel(
        PluginConfiguration config,
        DeathrollTournamentService deathrollService,
        DeathrollBettingService bettingService,
        DeathrollWebhookService discordService,
        DrtWebviewService webviewService,
        ChatQueueService chatQueue,
        SessionService sessionService,
        IPluginLog log,
        HistoryService historyService,
        AutoPayoutService autoPayoutService)
    {
        this.config              = config;
        this.deathrollService    = deathrollService;
        this.sessionService      = sessionService;
        this.webviewService      = webviewService;
        this.chatQueue           = chatQueue;
        this.bracketTab          = new DeathrollBracketTab(config, deathrollService, bettingService, chatQueue, autoPayoutService, webviewService);
        this.chatSettingsTab     = new DeathrollChatSettingsTab(config);
        this.settingsTab         = new DeathrollSettingsTab(config);
        this.statsTab            = new DeathrollStatsTab(config, deathrollService, chatQueue, historyService);
        this.betsTab             = new DeathrollBetsTab(config, deathrollService, bettingService, chatQueue, autoPayoutService);
        this.discordTab          = new DeathrollDiscordWebhookTab(config, discordService, log);
        this.webviewTab          = new DeathrollWebviewTab(config, webviewService);
        this.chatAutomation      = new DeathrollChatAutomation(config, deathrollService, bettingService, chatQueue);
        this.nextMatchAutomation = new DeathrollNextMatchAutomation(config, deathrollService);
    }

    public void Dispose()
    {
        this.chatAutomation.Dispose();
        this.nextMatchAutomation.Dispose();
    }

    public static IReadOnlyList<GameSection> Sections { get; } =
    [
        GameSection.Game, GameSection.Betting, GameSection.Chat,
        GameSection.Settings, GameSection.Webview, GameSection.Discord,
    ];
    public void DrawSection(GameSection section)
    {
        ImGui.Spacing();
        switch (section)
        {
            case GameSection.Game:     DrawGameSection(); break;
            case GameSection.Chat:     DrawChatSection(); break;
            case GameSection.Betting:  DrawBettingSection(); break;
            case GameSection.Settings: DrawSettingsSection(); break;
            case GameSection.Discord:  DrawDiscordSection(); break;
            case GameSection.Webview:  DrawWebviewSection(); break;
        }
    }

    private void DrawGameSection()
    {
        if (!this.deathrollService.IsSessionActive())
        {
            DrawStartDoor();
            return;
        }
        ImGui.Spacing();
        DrawShouts();
        var statsH = DeathrollStatsTab.GetInlineHeight(this.deathrollService.IsGilPrize());
        this.bracketTab.Draw(
            skipLeadingSpacing: true,
            reserveBottom: statsH,
            drawStatsInline: this.statsTab.DrawInline);
    }

    private void DrawShouts()
    {
        using (var sections = ImRaii.Table("##DRShoutSections", 2, ImGuiTableFlags.BordersInnerV))
        {
            if (sections.Success)
            {
                ImGui.TableSetupColumn("##DRGameShoutCol",  ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##DRMatchShoutCol", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                DrawGameShouts();
                ImGui.TableSetColumnIndex(1);
                this.bracketTab.DrawMatchShouts();
            }
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private void DrawGameShouts()
    {
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Game Shouts");
        ImGui.Spacing();
        var row = new ShoutButtonRow();

        var state = this.config.DeathrollTournamentSession;
        if (state != null && state.Rounds.Count > 0)
        {
            using (UIHelper.PushYellowButtonColours())
                if (row.Button(FontAwesomeIcon.Sitemap, $"Announce {state.CurrentRoundLabel()} Bracket", "##DRShoutBracket"))
                    AnnounceBracket.Execute(this.config, this.chatQueue, state);
        }

        using (UIHelper.PushBlueButtonColours())
            if (row.Button(FontAwesomeIcon.Trophy, "Announce Prize", "##DRShoutPrize"))
                AnnouncePrize.Execute(this.config, this.chatQueue);

        if (this.webviewService.SessionId != null)
        {
            using (UIHelper.PushGreenButtonColours())
                if (row.Button(FontAwesomeIcon.Globe, "Announce Web Link", "##DRShoutWebLink"))
                    AnnounceWebviewLink.Execute(this.config, this.chatQueue);
        }
    }

    private void DrawChatSection()
    {
        using var scroll = ImRaii.Child("##DRChatScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.chatSettingsTab.Draw();
    }

    private void DrawBettingSection()
    {
        this.betsTab.Draw();
    }

    private void DrawSettingsSection()
    {
        this.settingsTab.Draw();
    }

    private void DrawDiscordSection()
    {
        using var scroll = ImRaii.Child("##DRDiscordScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.discordTab.Draw();
    }

    private void DrawWebviewSection()
    {
        using var scroll = ImRaii.Child("##DRWebviewScroll", new Vector2(-1f, -1f), false);
        if (scroll.Success) this.webviewTab.Draw();
    }

    private void DrawStartDoor()
    {
        var blocking = this.sessionService.GetBlockingGameName("Deathroll Tournament");
        if (blocking != null)
        {
            DrawActiveBlockingDoor(blocking);
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

    private void DrawActiveBlockingDoor(string blockingGameName)
    {
        GameSessionDoorHost.Draw(
            KnownGameDoorModules.DeathrollTournament,
            DoorSurfaceBlocking,
            ref this.trackedBlockingDoorSpanPx,
            GameSessionDoorStyles.DeathrollTournamentBlockingDoor,
            () => DrawActiveBlockingDoorBody(blockingGameName));
    }

    private void DrawActiveBlockingDoorBody(string blockingGameName)
    {
        var wrapEnd = ImGui.GetCursorPos().X + MathF.Max(8f, ImGui.GetContentRegionAvail().X);
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextColored(EmporiumNeonTheme.WarningPanel,
            $"{blockingGameName} is currently running. Please end or discard the game to play Deathroll Tournament.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Discard Session", "##DRDiscardSession"))
            this.sessionService.CancelActiveGame();
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
        UIHelper.DrawStartSessionHeading(EmporiumNeonTheme.DeathrollTournamentPink);
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
