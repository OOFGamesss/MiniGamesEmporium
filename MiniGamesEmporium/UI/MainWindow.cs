using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Games.DeathrollTournament.Discord;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Games.DeathrollTournament.Webview;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Games.VotingMadness.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.UI.Tabs;
using System;
using System.Linq;
using System.Numerics;

/// <summary>The main plugin window hosting the navigation sidebar and the selected section's content.</summary>

namespace MiniGamesEmporium.UI;
public sealed class MainWindow : Window, IDisposable
{
    private enum Tab
    {
        MiniGames,
        SessionHistory,
        TransactionHistory,
        PresetManager,
        Settings,
        Support,
    }

    private enum SettingsSection
    {
        WebviewSetup,
        OtherSettings,
    }

    private enum SupportSection
    {
        Help,
        GameKey,
    }

    private const int PreDrawColourCount = 5;
    private const float MinSidebarWidth = 190f;
    private const float MinContentWidth = 400f;
    private const float MinWindowHeight = 320f;
    private const float GameIndent = 18f;
    private const float SectionIndent = 36f;
    private const string ActivePillLabel = "ACTIVE";
    private const string PausedPillLabel = "PAUSED";
    private const string LivePillLabel = "LIVE";
    private const string PausedDoorSurface = "PausedDoor";
    private const string ContinueSessionLabel = "Continue Session";
    private const string StopSessionLabel = "Stop Session";

    private static readonly string[] SubItemLabels =
        ["Webview Setup", "Other Settings", "Help", "Game Key"];

    private static readonly Vector4 LivePillColour = new(0.15f, 0.65f, 0.25f, 1f);
    private static readonly Vector4 PausedPillColour = new(0.72f, 0.52f, 0.04f, 1f);
    private static readonly Vector4 ButtonText = new(1f, 0.98f, 0.98f, 1f);
    private static readonly Vector4 StopHovered = new(1f, 0.22f, 0.38f, 1f);
    private static readonly Vector4 StopActive = new(0.72f, 0.08f, 0.22f, 1f);
    private static readonly Vector4 ResumeButton = new(0.04f, 0.38f, 0.08f, 1f);
    private static readonly Vector4 ResumeHovered = new(0.08f, 0.72f, 0.15f, 1f);
    private static readonly Vector4 ResumeActive = new(0.06f, 0.52f, 0.10f, 1f);
    private static readonly Vector4 PauseButton = new(0.62f, 0.56f, 0.03f, 1f);
    private static readonly Vector4 PauseHovered = new(0.78f, 0.72f, 0.04f, 1f);
    private static readonly Vector4 PauseActive = new(0.48f, 0.44f, 0.02f, 1f);

    public event Action? WindowOpened;

    private readonly TransactionHistoryTab transactionHistoryTab;
    private readonly SessionHistoryTab sessionHistoryTab;
    private readonly MiniGamesTab miniGamesTab;
    private readonly PresetManagerTab presetManagerTab;
    private readonly SettingsTab settingsTab;
    private readonly SupportTab supportTab;
    private readonly GameKeyTab gameKeyTab;
    private readonly Bar777SessionService bar777SessionService;
    private readonly SessionService sessionService;
    private readonly DeathrollTournamentService deathrollService;
    private readonly HigherLowerService higherLowerService;
    private readonly RaffleService raffleService;
    private readonly VotingMadnessService votingMadnessService;
    private readonly CoinCollectorService coinCollectorService;
    private readonly DrtWebviewService drtWebviewService;
    private readonly PluginConfiguration config;

    private Tab selected = Tab.MiniGames;
    private MiniGame selectedGame = MiniGame.Bar777;
    private GameSection selectedSection = GameSection.Game;
    private SettingsSection settingsSection = SettingsSection.WebviewSetup;
    private SupportSection supportSection = SupportSection.Help;
    private MiniGame? expandedGame = MiniGame.Bar777;
    private bool miniGamesExpanded = true;
    private bool settingsExpanded;
    private bool supportExpanded;
    private bool pendingStopConfirm;
    private bool pendingDeathrollStopConfirm;
    private bool pendingHLStopConfirm;
    private bool pendingRaffleStopConfirm;
    private bool pendingVotingMadnessStopConfirm;
    private bool pendingCoinCollectorStopConfirm;
    private float trackedPausedDoorSpanPx = GameSessionDoorStyles.PausedSessionDoor.SeedTrackedContentSpanPx;
    private float lastSidebarWidth = MinSidebarWidth;

    public MainWindow(
        PluginConfiguration config,
        Bar777SessionService bar777SessionService,
        SessionService sessionService,
        ChatQueueService chatQueue,
        DeathrollTournamentService deathrollService,
        DeathrollBettingService bettingService,
        DeathrollWebhookService deathrollDiscordService,
        DrtWebviewService drtWebviewService,
        PresetService presetService,
        IPluginLog log,
        HistoryService historyService,
        AutoPayoutService autoPayoutService,
        HigherLowerService higherLowerService,
        PlayerInfoService playerInfoService,
        RaffleService raffleService,
        VotingMadnessService votingMadnessService,
        CoinCollectorService coinCollectorService)
        : base("Mini Games Emporium##MGE_Main_v2")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(MinSidebarWidth + MinContentWidth, MinWindowHeight),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        this.transactionHistoryTab = new TransactionHistoryTab(historyService);
        this.sessionHistoryTab     = new SessionHistoryTab(historyService);
        this.miniGamesTab          = new MiniGamesTab(config, bar777SessionService, chatQueue, deathrollService, bettingService, deathrollDiscordService, drtWebviewService, log, historyService, autoPayoutService, higherLowerService, playerInfoService, raffleService, votingMadnessService, coinCollectorService);
        this.presetManagerTab      = new PresetManagerTab(config, presetService);
        this.settingsTab           = new SettingsTab(config);
        this.supportTab            = new SupportTab();
        this.gameKeyTab            = new GameKeyTab();
        this.bar777SessionService  = bar777SessionService;
        this.sessionService        = sessionService;
        this.deathrollService      = deathrollService;
        this.higherLowerService    = higherLowerService;
        this.raffleService         = raffleService;
        this.votingMadnessService  = votingMadnessService;
        this.coinCollectorService  = coinCollectorService;
        this.drtWebviewService     = drtWebviewService;
        this.config                = config;
    }

    public override void OnOpen() => WindowOpened?.Invoke();

    public void OpenSettingsTab()
    {
        IsOpen = true;
        this.selected = Tab.Settings;
        this.settingsSection = SettingsSection.WebviewSetup;
        this.settingsExpanded = true;
    }

    public void Dispose()
    {
        this.miniGamesTab.Dispose();
    }

    public override void PreDraw()
    {
        ApplySizeConstraints();
        var deep = new Vector4(0.04f, 0.02f, 0.07f, 0.97f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg,         deep);
        ImGui.PushStyleColor(ImGuiCol.TitleBg,          deep);
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive,    EmporiumNeonTheme.EmporiumPurpleActive);
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, deep);
        ImGui.PushStyleColor(ImGuiCol.Border,           new Vector4(0.42f, 0.08f, 0.62f, 0.60f));
    }

    private void ApplySizeConstraints()
    {
        var scale   = ImGuiHelpers.GlobalScale;
        var sidebar = UiLayoutState.HideNavSidebar ? 0f : this.lastSidebarWidth;
        var chrome  = PanelEdgeTag.Size() + ImGui.GetStyle().ItemSpacing.X;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(sidebar + chrome + MinContentWidth * scale, MinWindowHeight * scale),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(PreDrawColourCount);
    }

    public override void Draw()
    {
        using var theme = new EmporiumNeonTheme.Scope();

        var shellHeight = ImGui.GetContentRegionAvail().Y;

        if (!UiLayoutState.HideNavSidebar)
        {
            this.lastSidebarWidth = CalcSidebarWidth();
            using (var sidebar = ImRaii.Child("##MGE_Sidebar", new Vector2(this.lastSidebarWidth, 0f), true))
            {
                if (sidebar.Success)
                    DrawSidebar();
            }

            ImGui.SameLine(0f, 0f);
        }

        DrawSidebarEdgeTag(shellHeight);
        ImGui.SameLine(0f, ImGui.GetStyle().ItemSpacing.X);

        using (var content = ImRaii.Child(
                   "##MGE_Content",
                   Vector2.Zero,
                   false,
                   ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (content.Success)
            {
                DrawSessionControls();
                using (var body = ImRaii.Child(
                           "##MGE_ContentBody",
                           Vector2.Zero,
                           false,
                           ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    if (body.Success)
                        DrawSelectedContent();
                }
            }
        }

        DrawStopConfirmPopups();
    }

    private float CalcSidebarWidth()
    {
        var style = ImGui.GetStyle();
        var scale = ImGuiHelpers.GlobalScale;

        var widest = SidebarRow.CalcRowWidth("Transaction History", 0f, false);
        widest = MathF.Max(widest, SidebarRow.CalcRowWidth("Mini Games", 0f, true));

        foreach (var label in SubItemLabels)
            widest = MathF.Max(widest, SidebarRow.CalcRowWidth(label, GameIndent * scale, false));

        foreach (var entry in this.miniGamesTab.NavEntries)
        {
            var gamePill = entry.Sections.Count > 0 ? ActivePillLabel : null;
            widest = MathF.Max(widest, SidebarRow.CalcRowWidth(entry.Label(), GameIndent * scale, entry.Sections.Count > 0, gamePill));

            foreach (var section in entry.Sections)
            {
                var sectionPill = section == GameSection.Webview ? LivePillLabel : null;
                widest = MathF.Max(widest, SidebarRow.CalcRowWidth(section.Label(), SectionIndent * scale, false, sectionPill));
            }
        }

        var width = widest + style.WindowPadding.X * 2f + style.ScrollbarSize;
        return MathF.Max(width, MinSidebarWidth * scale);
    }

    private void DrawSidebar()
    {
        ImGuiHelpers.ScaledDummy(4f);
        DrawMiniGamesNav();

        ImGuiHelpers.ScaledDummy(4f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(4f);

        DrawNavItem(Tab.SessionHistory,     FontAwesomeIcon.History,     "Session History");
        DrawNavItem(Tab.TransactionHistory, FontAwesomeIcon.ExchangeAlt, "Transaction History");
        DrawNavItem(Tab.PresetManager,      FontAwesomeIcon.SlidersH,    "Preset Manager");

        ImGuiHelpers.ScaledDummy(4f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(4f);

        DrawSettingsNav();
        DrawSupportNav();
    }

    private void DrawNavItem(Tab tab, FontAwesomeIcon icon, string label)
    {
        if (SidebarRow.Draw($"tab_{tab}", icon, label, this.selected == tab))
            this.selected = tab;
    }

    private void DrawMiniGamesNav()
    {
        var chevron = this.miniGamesExpanded ? FontAwesomeIcon.ChevronDown : FontAwesomeIcon.ChevronRight;
        if (SidebarRow.Draw("tab_MiniGames", FontAwesomeIcon.Gamepad, "Mini Games", false, 0f, chevron))
            this.miniGamesExpanded = !this.miniGamesExpanded;

        if (!this.miniGamesExpanded)
            return;

        foreach (var entry in this.miniGamesTab.NavEntries)
            DrawGameNav(entry);
    }

    private void DrawGameNav(MiniGameNavEntry entry)
    {
        var hasSections = entry.Sections.Count > 0;
        var expanded = this.expandedGame == entry.Game;
        var isSelected = this.selected == Tab.MiniGames && this.selectedGame == entry.Game;

        FontAwesomeIcon? chevron = hasSections
            ? expanded ? FontAwesomeIcon.ChevronDown : FontAwesomeIcon.ChevronRight
            : null;

        var (pill, pillColour) = GameSessionPill(entry.Game);

        if (SidebarRow.Draw($"game_{entry.Game}", entry.Icon, entry.Label(), isSelected,
                GameIndent * ImGuiHelpers.GlobalScale, chevron, entry.Colour, pill, pillColour))
        {
            this.selected = Tab.MiniGames;
            this.selectedGame = entry.Game;
            this.selectedSection = GameSection.Game;
            this.expandedGame = hasSections && !expanded ? entry.Game : null;
        }

        if (pill != null && ImGui.IsItemHovered())
            ImGui.SetTooltip(pill == PausedPillLabel ? "Session is paused." : "Session is running.");

        if (this.expandedGame != entry.Game)
            return;

        foreach (var section in entry.Sections)
            DrawGameSectionNav(entry, section);
    }

    private void DrawGameSectionNav(MiniGameNavEntry entry, GameSection section)
    {
        var isSelected = this.selected == Tab.MiniGames
            && this.selectedGame == entry.Game
            && this.selectedSection == section;

        var live = IsWebviewLive(entry.Game, section);
        var pill = live ? LivePillLabel : null;
        var pillColour = live ? Pulse(LivePillColour) : (Vector4?)null;

        var clicked = SidebarRow.Draw($"section_{entry.Game}_{section}", section.Icon(), section.Label(), isSelected,
            SectionIndent * ImGuiHelpers.GlobalScale, null, entry.Colour, pill, pillColour);

        if (live && ImGui.IsItemHovered())
            ImGui.SetTooltip("Web session is live.");

        if (!clicked)
            return;

        this.selected = Tab.MiniGames;
        this.selectedGame = entry.Game;
        this.selectedSection = section;
    }

    private (string? Pill, Vector4? Colour) GameSessionPill(MiniGame game)
    {
        if (!IsSessionActive(game))
            return (null, null);

        return this.sessionService.IsFocused(DisplayNameOf(game))
            ? (ActivePillLabel, Pulse(LivePillColour))
            : (PausedPillLabel, PausedPillColour);
    }

    private static Vector4 Pulse(Vector4 colour)
    {
        var pulse = (float)(0.5 + 0.5 * Math.Sin(ImGui.GetTime() * 3.0));
        return colour with { W = 0.45f + 0.55f * pulse };
    }

    private bool IsWebviewLive(MiniGame game, GameSection section) =>
        game == MiniGame.DeathrollTournament
        && section == GameSection.Webview
        && this.drtWebviewService.SessionId != null;

    private bool IsSessionActive(MiniGame game) => game switch
    {
        MiniGame.Bar777              => IsBar777SessionActive(),
        MiniGame.DeathrollTournament => this.deathrollService.IsSessionActive(),
        MiniGame.HigherLower         => this.higherLowerService.IsSessionActive(),
        MiniGame.Raffle              => this.raffleService.IsSessionActive(),
        MiniGame.VotingMadness       => this.votingMadnessService.IsSessionActive(),
        MiniGame.CoinCollector       => this.coinCollectorService.IsSessionActive(),
        _ => false,
    };

    private static string DisplayNameOf(MiniGame game) => game switch
    {
        MiniGame.Bar777              => Bar777GameIds.DisplayName,
        MiniGame.DeathrollTournament => DeathrollGameIds.DisplayName,
        MiniGame.HigherLower         => HigherLowerGameIds.DisplayName,
        MiniGame.Raffle              => RaffleGameIds.DisplayName,
        MiniGame.VotingMadness       => VotingMadnessGameIds.DisplayName,
        MiniGame.CoinCollector       => CoinCollectorGameIds.DisplayName,
        _ => string.Empty,
    };

    private void DrawSettingsNav()
    {
        var chevron = this.settingsExpanded ? FontAwesomeIcon.ChevronDown : FontAwesomeIcon.ChevronRight;
        if (SidebarRow.Draw("tab_Settings", FontAwesomeIcon.Cog, "Settings", this.selected == Tab.Settings, 0f, chevron))
        {
            this.settingsExpanded = !this.settingsExpanded;
            if (this.settingsExpanded)
            {
                this.selected = Tab.Settings;
                this.settingsSection = SettingsSection.WebviewSetup;
            }
        }

        if (!this.settingsExpanded)
            return;

        DrawSettingsSubItem(SettingsSection.WebviewSetup,  FontAwesomeIcon.Globe,  "Webview Setup");
        DrawSettingsSubItem(SettingsSection.OtherSettings, FontAwesomeIcon.Wrench, "Other Settings");
    }

    private void DrawSettingsSubItem(SettingsSection section, FontAwesomeIcon icon, string label)
    {
        var isSelected = this.selected == Tab.Settings && this.settingsSection == section;
        if (!SidebarRow.Draw($"settings_{section}", icon, label, isSelected, GameIndent * ImGuiHelpers.GlobalScale))
            return;

        this.selected = Tab.Settings;
        this.settingsSection = section;
    }

    private void DrawSupportNav()
    {
        var chevron = this.supportExpanded ? FontAwesomeIcon.ChevronDown : FontAwesomeIcon.ChevronRight;
        if (SidebarRow.Draw("tab_Support", FontAwesomeIcon.QuestionCircle, "Support", this.selected == Tab.Support, 0f, chevron))
        {
            this.supportExpanded = !this.supportExpanded;
            if (this.supportExpanded)
            {
                this.selected = Tab.Support;
                this.supportSection = SupportSection.Help;
            }
        }

        if (!this.supportExpanded)
            return;

        DrawSupportSubItem(SupportSection.Help,    FontAwesomeIcon.InfoCircle, "Help");
        DrawSupportSubItem(SupportSection.GameKey, FontAwesomeIcon.Key,        "Game Key");
    }

    private void DrawSupportSubItem(SupportSection section, FontAwesomeIcon icon, string label)
    {
        var isSelected = this.selected == Tab.Support && this.supportSection == section;
        if (!SidebarRow.Draw($"support_{section}", icon, label, isSelected, GameIndent * ImGuiHelpers.GlobalScale))
            return;

        this.selected = Tab.Support;
        this.supportSection = section;
    }

    private void DrawSelectedContent()
    {
        switch (this.selected)
        {
            case Tab.MiniGames:          DrawMiniGamesContent(); break;
            case Tab.SessionHistory:     this.sessionHistoryTab.Draw(); break;
            case Tab.TransactionHistory: this.transactionHistoryTab.Draw(); break;
            case Tab.PresetManager:      this.presetManagerTab.Draw(); break;
            case Tab.Settings:           DrawSettingsContent(); break;
            case Tab.Support:            DrawSupportContent(); break;
        }
    }

    private void DrawMiniGamesContent()
    {
        if (this.selectedSection == GameSection.Game && IsSessionPaused(this.selectedGame))
        {
            DrawPausedSessionDoor(this.selectedGame);
            return;
        }
        this.miniGamesTab.Draw(this.selectedGame, this.selectedSection);
    }

    private bool IsSessionPaused(MiniGame game) =>
        IsSessionActive(game) && !this.sessionService.IsFocused(DisplayNameOf(game));

    private void DrawPausedSessionDoor(MiniGame game)
    {
        GameSessionDoorHost.Draw(
            game.ToString(),
            PausedDoorSurface,
            ref this.trackedPausedDoorSpanPx,
            GameSessionDoorStyles.PausedSessionDoor,
            () => DrawPausedSessionDoorBody(game));
    }

    private void DrawPausedSessionDoorBody(MiniGame game)
    {
        var entry  = this.miniGamesTab.NavEntries.FirstOrDefault(e => e.Game == game);
        var label  = entry?.Label() ?? DisplayNameOf(game);
        var colour = entry?.Colour ?? EmporiumNeonTheme.WarningPanel;

        ImGui.TextColored(colour, "Session Paused");
        ImGui.Separator();
        ImGui.Spacing();

        var focused = this.sessionService.FocusedGameName;
        var message = focused != null
            ? $"{label} still has a live session, but {focused} currently has control. Continue Session to move chat and trades to {label}."
            : $"{label} still has a live session. Continue Session to resume chat listening and trades.";

        var wrapEnd = ImGui.GetCursorPos().X + MathF.Max(8f, ImGui.GetContentRegionAvail().X);
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextUnformatted(message);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();

        var style       = ImGui.GetStyle();
        var continueW   = UIHelper.CalcButtonSize(FontAwesomeIcon.Play, ContinueSessionLabel).X;
        var stopW       = UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, StopSessionLabel).X;
        var totalWidth  = continueW + style.ItemSpacing.X + stopW;
        var avail       = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, (avail - totalWidth) * 0.5f));

        if (DrawColouredButton(FontAwesomeIcon.Play, ContinueSessionLabel, "##MGE_PausedDoorContinue",
                ResumeButton, ResumeHovered, ResumeActive, EmporiumNeonTheme.MinefieldGreenDim))
            this.sessionService.Focus(DisplayNameOf(game));

        ImGui.SameLine();

        if (DrawColouredButton(FontAwesomeIcon.Stop, StopSessionLabel, "##MGE_PausedDoorStop",
                EmporiumNeonTheme.Bar777Red, StopHovered, StopActive, EmporiumNeonTheme.Bar777RedDim))
            RequestStopConfirm(game);
    }

    private void RequestStopConfirm(MiniGame game)
    {
        switch (game)
        {
            case MiniGame.Bar777:              this.pendingStopConfirm = true; break;
            case MiniGame.DeathrollTournament: this.pendingDeathrollStopConfirm = true; break;
            case MiniGame.HigherLower:         this.pendingHLStopConfirm = true; break;
            case MiniGame.Raffle:              this.pendingRaffleStopConfirm = true; break;
            case MiniGame.VotingMadness:       this.pendingVotingMadnessStopConfirm = true; break;
            case MiniGame.CoinCollector:       this.pendingCoinCollectorStopConfirm = true; break;
        }
    }

    private void DrawSupportContent()
    {
        switch (this.supportSection)
        {
            case SupportSection.Help:    this.supportTab.Draw(); break;
            case SupportSection.GameKey: this.gameKeyTab.Draw(); break;
        }
    }

    private void DrawSettingsContent()
    {
        switch (this.settingsSection)
        {
            case SettingsSection.WebviewSetup:  this.settingsTab.DrawWebviewSetupSection(); break;
            case SettingsSection.OtherSettings: this.settingsTab.DrawOtherSettingsSection(); break;
        }
    }

    private void DrawSidebarEdgeTag(float height)
    {
        var hidden = UiLayoutState.HideNavSidebar;
        if (!PanelEdgeTag.DrawVertical("##MGE_SidebarTag", height, hidden, EmporiumNeonTheme.EmporiumPurpleActive, "the navigation sidebar", panelOnRight: false))
            return;
        UiLayoutState.HideNavSidebar = !hidden;
    }

    private void DrawSessionControls()
    {
        if (this.selected != Tab.MiniGames || !IsSessionActive(this.selectedGame))
            return;

        var idSuffix = this.selectedGame switch
        {
            MiniGame.Bar777              => "Bar777",
            MiniGame.DeathrollTournament => "Deathroll",
            MiniGame.HigherLower         => "HL",
            MiniGame.Raffle              => "Raffle",
            MiniGame.VotingMadness       => "VM",
            MiniGame.CoinCollector       => "CC",
            _ => null,
        };
        if (idSuffix == null) return;
        DrawSessionControlStrip(this.selectedGame, DisplayNameOf(this.selectedGame), idSuffix, () => RequestStopConfirm(this.selectedGame));
    }

    private bool IsBar777SessionActive()
    {
        var session = this.bar777SessionService.GetActiveSession();
        return session != null && Bar777GameIds.Matches(session.GameName);
    }

    private void DrawSessionControlStrip(MiniGame game, string gameName, string idSuffix, Action onStop)
    {
        var isPaused   = !this.sessionService.IsFocused(gameName);
        var pauseLabel = isPaused ? ContinueSessionLabel : "Pause Session";
        var pauseIcon  = isPaused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;

        var style = ImGui.GetStyle();
        var totalWidth = UIHelper.CalcButtonSize(pauseIcon, pauseLabel).X
            + style.ItemSpacing.X
            + UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, StopSessionLabel).X;

        var rightEdge = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;

        if (!isPaused && this.miniGamesTab.DrawSessionActionButtons(game))
            ImGui.SameLine();

        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), rightEdge - totalWidth));

        var pauseClicked = isPaused
            ? DrawColouredButton(pauseIcon, pauseLabel, "##MGE_Pause" + idSuffix, ResumeButton, ResumeHovered, ResumeActive, EmporiumNeonTheme.MinefieldGreenDim)
            : DrawColouredButton(pauseIcon, pauseLabel, "##MGE_Pause" + idSuffix, PauseButton, PauseHovered, PauseActive, EmporiumNeonTheme.PauseYellowDim);

        if (pauseClicked)
        {
            if (isPaused) this.sessionService.Focus(gameName);
            else this.sessionService.ClearFocus();
        }

        ImGui.SameLine();

        if (DrawColouredButton(FontAwesomeIcon.Stop, StopSessionLabel, "##MGE_Stop" + idSuffix,
                EmporiumNeonTheme.Bar777Red, StopHovered, StopActive, EmporiumNeonTheme.Bar777RedDim))
            onStop();

        ImGui.Separator();
    }

    private static bool DrawColouredButton(FontAwesomeIcon icon, string label, string id, Vector4 button, Vector4 hovered, Vector4 active, Vector4 border)
    {
        ImGui.PushStyleColor(ImGuiCol.Button,        button);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  active);
        ImGui.PushStyleColor(ImGuiCol.Border,        border);
        ImGui.PushStyleColor(ImGuiCol.Text,          ButtonText);
        try
        {
            return UIHelper.IconTextButton(icon, label, id);
        }
        finally
        {
            ImGui.PopStyleColor(5);
        }
    }

    private void DrawStopConfirmPopups()
    {
        var bar777Message = this.config.Bar777.UseQueue
            ? "The queue will be cleared and all session stats will be reset."
            : "All session stats will be reset.";

        DrawStopConfirmPopup(ref this.pendingStopConfirm, "##StopSessionConfirm", EmporiumNeonTheme.Bar777Red,
            "Stop BAR 777 session?", bar777Message, "Stop Session", this.bar777SessionService.CancelSession);

        DrawStopConfirmPopup(ref this.pendingDeathrollStopConfirm, "##DeathrollStopConfirm", EmporiumNeonTheme.DeathrollTournamentPink,
            "Stop Deathroll Tournament?", "The tournament bracket and all session data will be cleared.", StopSessionLabel, this.deathrollService.StopSession);

        DrawStopConfirmPopup(ref this.pendingHLStopConfirm, "##HLStopConfirm", EmporiumNeonTheme.HigherLowerOrange,
            "Stop Higher/Lower session?", "All session stats will be reset.", "Stop Session", this.higherLowerService.CancelSession);

        DrawStopConfirmPopup(ref this.pendingRaffleStopConfirm, "##RaffleStopConfirm", EmporiumNeonTheme.RaffleTeal,
            "Stop Session?", "The ticket pool, players and draw result will all be cleared.", "Stop Session", this.raffleService.StopSession);

        DrawStopConfirmPopup(ref this.pendingVotingMadnessStopConfirm, "##VMStopConfirm", EmporiumNeonTheme.VotingMadnessLime,
            "Stop Session?", "All votes and session data will be cleared and written to session history.", "Stop Session", this.votingMadnessService.StopSession);

        DrawStopConfirmPopup(ref this.pendingCoinCollectorStopConfirm, "##CCStopConfirm", EmporiumNeonTheme.CoinCollectorIndigo,
            "Stop Coin Collector session?", "All session stats will be reset.", "Stop Session", this.coinCollectorService.CancelSession);
    }

    private void DrawStopConfirmPopup(ref bool pending, string id, Vector4 titleColour, string title, string message, string stopLabel, Action onConfirm)
    {
        if (pending)
        {
            ImGui.OpenPopup(id);
            pending = false;
        }

        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.Popup(id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;

        ImGui.TextColored(titleColour, title);
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted(message);
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, stopLabel, $"##Confirm{id}"))
        {
            onConfirm();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", $"##Cancel{id}"))
            ImGui.CloseCurrentPopup();
    }
}
