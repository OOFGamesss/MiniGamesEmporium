using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Discord;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Webview;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.UI.Tabs;
using System;
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
    private const float MinContentWidth = 680f;
    private const float GameIndent = 18f;
    private const float SectionIndent = 36f;
    private const string ActivePillLabel = "ACTIVE";
    private const string PausedPillLabel = "PAUSED";
    private const string LivePillLabel = "LIVE";

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
        VotingMadnessService votingMadnessService)
        : base("Mini Games Emporium##MGE_Main_v2")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(MinSidebarWidth + MinContentWidth, 450),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        this.transactionHistoryTab = new TransactionHistoryTab(historyService);
        this.sessionHistoryTab     = new SessionHistoryTab(historyService);
        this.miniGamesTab          = new MiniGamesTab(config, bar777SessionService, sessionService, chatQueue, deathrollService, bettingService, deathrollDiscordService, drtWebviewService, log, historyService, autoPayoutService, higherLowerService, playerInfoService, raffleService, votingMadnessService);
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
        var deep = new Vector4(0.04f, 0.02f, 0.07f, 0.97f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg,         deep);
        ImGui.PushStyleColor(ImGuiCol.TitleBg,          deep);
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive,    EmporiumNeonTheme.EmporiumPurpleActive);
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, deep);
        ImGui.PushStyleColor(ImGuiCol.Border,           new Vector4(0.42f, 0.08f, 0.62f, 0.60f));
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(PreDrawColourCount);
    }

    public override void Draw()
    {
        using var theme = new EmporiumNeonTheme.Scope();

        using (var sidebar = ImRaii.Child("##MGE_Sidebar", new Vector2(CalcSidebarWidth(), 0f), true))
        {
            if (sidebar.Success)
                DrawSidebar();
        }

        ImGui.SameLine();

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

        return this.sessionService.IsPaused
            ? (PausedPillLabel, PausedPillColour)
            : (ActivePillLabel, Pulse(LivePillColour));
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
        _ => false,
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
            case Tab.MiniGames:          this.miniGamesTab.Draw(this.selectedGame, this.selectedSection); break;
            case Tab.SessionHistory:     this.sessionHistoryTab.Draw(); break;
            case Tab.TransactionHistory: this.transactionHistoryTab.Draw(); break;
            case Tab.PresetManager:      this.presetManagerTab.Draw(); break;
            case Tab.Settings:           DrawSettingsContent(); break;
            case Tab.Support:            DrawSupportContent(); break;
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

    private void DrawSessionControls()
    {
        if (this.selected != Tab.MiniGames || !IsSessionActive(this.selectedGame))
            return;

        switch (this.selectedGame)
        {
            case MiniGame.Bar777:
                DrawSessionControlStrip("Stop Session", "Session", "Bar777", () => this.pendingStopConfirm = true);
                break;
            case MiniGame.DeathrollTournament:
                DrawSessionControlStrip("Stop Tournament", "Tournament", "Deathroll", () => this.pendingDeathrollStopConfirm = true);
                break;
            case MiniGame.HigherLower:
                DrawSessionControlStrip("Stop Session", "Session", "HL", () => this.pendingHLStopConfirm = true);
                break;
            case MiniGame.Raffle:
                DrawSessionControlStrip("Stop Session", "Session", "Raffle", () => this.pendingRaffleStopConfirm = true);
                break;
            case MiniGame.VotingMadness:
                DrawSessionControlStrip("Stop Session", "Session", "VM", () => this.pendingVotingMadnessStopConfirm = true);
                break;
        }
    }

    private bool IsBar777SessionActive()
    {
        var session = this.bar777SessionService.GetActiveSession();
        return session != null && Bar777GameIds.Matches(session.GameName);
    }

    private void DrawSessionControlStrip(string stopLabel, string pauseNoun, string idSuffix, Action onStop)
    {
        var isPaused   = this.sessionService.IsPaused;
        var pauseLabel = isPaused ? $"Continue {pauseNoun}" : $"Pause {pauseNoun}";
        var pauseIcon  = isPaused ? FontAwesomeIcon.Play : FontAwesomeIcon.Pause;

        var style = ImGui.GetStyle();
        var totalWidth = UIHelper.CalcButtonSize(pauseIcon, pauseLabel).X
            + style.ItemSpacing.X
            + UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, stopLabel).X;

        var rightEdge = ImGui.GetWindowContentRegionMin().X + ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), rightEdge - totalWidth));

        var pauseClicked = isPaused
            ? DrawColouredButton(pauseIcon, pauseLabel, "##MGE_Pause" + idSuffix, ResumeButton, ResumeHovered, ResumeActive, EmporiumNeonTheme.MinefieldGreenDim)
            : DrawColouredButton(pauseIcon, pauseLabel, "##MGE_Pause" + idSuffix, PauseButton, PauseHovered, PauseActive, EmporiumNeonTheme.PauseYellowDim);

        if (pauseClicked)
        {
            if (isPaused) this.sessionService.ResumeActiveGame();
            else this.sessionService.PauseActiveGame();
        }

        ImGui.SameLine();

        if (DrawColouredButton(FontAwesomeIcon.Stop, stopLabel, "##MGE_Stop" + idSuffix,
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
            "Stop Deathroll Tournament?", "The tournament bracket and all session data will be cleared.", "Stop Tournament", this.deathrollService.StopSession);

        DrawStopConfirmPopup(ref this.pendingHLStopConfirm, "##HLStopConfirm", EmporiumNeonTheme.HigherLowerOrange,
            "Stop Higher/Lower session?", "All session stats will be reset.", "Stop Session", this.higherLowerService.CancelSession);

        DrawStopConfirmPopup(ref this.pendingRaffleStopConfirm, "##RaffleStopConfirm", EmporiumNeonTheme.RaffleTeal,
            "Stop Session?", "The ticket pool, players and draw result will all be cleared.", "Stop Session", this.raffleService.StopSession);

        DrawStopConfirmPopup(ref this.pendingVotingMadnessStopConfirm, "##VMStopConfirm", EmporiumNeonTheme.VotingMadnessLime,
            "Stop Session?", "All votes and session data will be cleared and written to session history.", "Stop Session", this.votingMadnessService.StopSession);
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
            this.sessionService.ResumeActiveGame();
            onConfirm();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", $"##Cancel{id}"))
            ImGui.CloseCurrentPopup();
    }
}
