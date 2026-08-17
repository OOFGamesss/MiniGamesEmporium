using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.UI;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Discord;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.UI;
using MiniGamesEmporium.Games.DeathrollTournament.Webview;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.Raffle.UI;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Games.MinefieldGambit.UI;
using MiniGamesEmporium.Games.HigherLower.UI;
using MiniGamesEmporium.Games.BeerPong.UI;
using MiniGamesEmporium.Games.Darts.UI;
using MiniGamesEmporium.Games.EightBallPool.UI;
using MiniGamesEmporium.Games.RussianRoulette.UI;
using MiniGamesEmporium.Games.RaidBoss.UI;
using MiniGamesEmporium.Games.DealOrNoDeal.UI;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Games.VotingMadness.UI;
using MiniGamesEmporium.UI.Components;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>Routes the sidebar's game selection to the matching game panel and hosts the win alert popup.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class MiniGamesTab : IDisposable
{
    private static readonly IReadOnlyList<GameSection> NoSections = [];

    private readonly Bar777Panel bar777Panel;
    private readonly DeathrollTournamentPanel deathrollTournamentPanel;
    private readonly RafflePanel rafflePanel;
    private readonly MinefieldGambitPanel minefieldGambitPanel;
    private readonly HigherLowerPanel higherLowerPanel;
    private readonly BeerPongPanel beerPongPanel;
    private readonly DartsPanel dartsPanel;
    private readonly EightBallPoolPanel eightBallPoolPanel;
    private readonly RussianRoulettePanel russianRoulettePanel;
    private readonly RaidBossPanel raidBossPanel;
    private readonly DealOrNoDealPanel dealOrNoDealPanel;
    private readonly VotingMadnessPanel votingMadnessPanel;
    private readonly Bar777SessionService bar777SessionService;
    private bool showWinAlert;
    private string winAlertPlayer = string.Empty;
    private int winAlertRoll;

    public IReadOnlyList<MiniGameNavEntry> NavEntries { get; }

    public MiniGamesTab(
        PluginConfiguration config,
        Bar777SessionService bar777SessionService,
        ChatQueueService chatQueue,
        DeathrollTournamentService deathrollService,
        DeathrollBettingService bettingService,
        DeathrollWebhookService deathrollDiscordService,
        DrtWebviewService drtWebviewService,
        IPluginLog log,
        HistoryService historyService,
        AutoPayoutService autoPayoutService,
        HigherLowerService higherLowerService,
        PlayerInfoService playerInfoService,
        RaffleService raffleService,
        VotingMadnessService votingMadnessService)
    {
        this.bar777SessionService     = bar777SessionService;
        this.bar777Panel              = new Bar777Panel(config, bar777SessionService, chatQueue, historyService, autoPayoutService);
        this.deathrollTournamentPanel = new DeathrollTournamentPanel(config, deathrollService, bettingService, deathrollDiscordService, drtWebviewService, chatQueue, log, historyService, autoPayoutService);
        this.rafflePanel              = new RafflePanel(config, raffleService, chatQueue, historyService, autoPayoutService, playerInfoService);
        this.minefieldGambitPanel     = new MinefieldGambitPanel();
        this.higherLowerPanel         = new HigherLowerPanel(config, higherLowerService, chatQueue, autoPayoutService, playerInfoService, historyService);
        this.beerPongPanel            = new BeerPongPanel();
        this.dartsPanel               = new DartsPanel();
        this.eightBallPoolPanel       = new EightBallPoolPanel();
        this.russianRoulettePanel     = new RussianRoulettePanel();
        this.raidBossPanel            = new RaidBossPanel();
        this.dealOrNoDealPanel        = new DealOrNoDealPanel();
        this.votingMadnessPanel       = new VotingMadnessPanel(config, votingMadnessService, chatQueue);
        this.NavEntries =
        [
            new(MiniGame.Bar777, () => config.Bar777.CustomName, FontAwesomeIcon.Dice, EmporiumNeonTheme.Bar777Red, Bar777Panel.Sections),
            new(MiniGame.DeathrollTournament, () => "Deathroll Tournament", FontAwesomeIcon.Skull, EmporiumNeonTheme.DeathrollTournamentPink, DeathrollTournamentPanel.Sections),
            new(MiniGame.Raffle, () => "Raffle", FontAwesomeIcon.TicketAlt, EmporiumNeonTheme.RaffleTeal, RafflePanel.Sections),
            new(MiniGame.HigherLower, () => "Higher/Lower", FontAwesomeIcon.ArrowsAltV, EmporiumNeonTheme.HigherLowerOrange, HigherLowerPanel.Sections),
            new(MiniGame.VotingMadness, () => "Voting Madness", FontAwesomeIcon.PollH, EmporiumNeonTheme.VotingMadnessLime, VotingMadnessPanel.Sections),
            new(MiniGame.MinefieldGambit, () => "Minefield Gambit", FontAwesomeIcon.Bomb, EmporiumNeonTheme.MinefieldGreen, NoSections),
            new(MiniGame.BeerPong, () => "Beer Pong", FontAwesomeIcon.Beer, EmporiumNeonTheme.BeerPongWhite, NoSections),
            new(MiniGame.Darts, () => "Darts", FontAwesomeIcon.Bullseye, EmporiumNeonTheme.DartsBlue, NoSections),
            new(MiniGame.EightBallPool, () => "8 Ball Pool", FontAwesomeIcon.Circle, EmporiumNeonTheme.EightBallPoolPurple, NoSections),
            new(MiniGame.RussianRoulette, () => "Russian Roulette", FontAwesomeIcon.Crosshairs, EmporiumNeonTheme.RussianRouletteCyan, NoSections),
            new(MiniGame.RaidBoss, () => "Raid Boss", FontAwesomeIcon.Dragon, EmporiumNeonTheme.RaidBossFuchsia, NoSections),
            new(MiniGame.DealOrNoDeal, () => "Deal or No Deal", FontAwesomeIcon.Briefcase, EmporiumNeonTheme.DealOrNoDealGold, NoSections),
        ];
        bar777SessionService.WinDetected += OnWinDetected;
    }

    private void OnWinDetected(string playerName, int roll)
    {
        this.showWinAlert = true;
        this.winAlertPlayer = playerName;
        this.winAlertRoll = roll;
    }

    public void Dispose()
    {
        this.bar777SessionService.WinDetected -= OnWinDetected;
        this.bar777Panel.Dispose();
        this.deathrollTournamentPanel.Dispose();
        this.rafflePanel.Dispose();
        this.higherLowerPanel.Dispose();
    }

    public void Draw(MiniGame game, GameSection section)
    {
        DrawWinAlertPopup();
        switch (game)
        {
            case MiniGame.Bar777:              this.bar777Panel.DrawSection(section); break;
            case MiniGame.DeathrollTournament: this.deathrollTournamentPanel.DrawSection(section); break;
            case MiniGame.Raffle:              this.rafflePanel.DrawSection(section); break;
            case MiniGame.HigherLower:         this.higherLowerPanel.DrawSection(section); break;
            case MiniGame.MinefieldGambit:     this.minefieldGambitPanel.Draw(); break;
            case MiniGame.BeerPong:            this.beerPongPanel.Draw(); break;
            case MiniGame.Darts:               this.dartsPanel.Draw(); break;
            case MiniGame.EightBallPool:       this.eightBallPoolPanel.Draw(); break;
            case MiniGame.RussianRoulette:     this.russianRoulettePanel.Draw(); break;
            case MiniGame.RaidBoss:            this.raidBossPanel.Draw(); break;
            case MiniGame.DealOrNoDeal:        this.dealOrNoDealPanel.Draw(); break;
            case MiniGame.VotingMadness:       this.votingMadnessPanel.DrawSection(section); break;
        }
    }

    private void DrawWinAlertPopup()
    {
        if (!this.showWinAlert) return;
        ImGui.OpenPopup("##WinAlert");
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.Popup("##WinAlert", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.WinGold, "WINNER!");
        ImGui.Separator();
        ImGui.Text($"{this.winAlertPlayer} rolled {this.winAlertRoll}!");
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Dismiss", "##WinDismiss"))
        {
            this.showWinAlert = false;
            ImGui.CloseCurrentPopup();
        }
    }
}
