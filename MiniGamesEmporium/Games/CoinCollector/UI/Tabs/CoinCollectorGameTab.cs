using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Actions;
using MiniGamesEmporium.Games.CoinCollector.Models;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the active game view for Coin Collector.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.UI.Tabs;
public sealed class CoinCollectorGameTab : IDisposable
{
    private const float RightPaneW    = 250f;
    private const float MinLogHeight  = 120f;
    private const float PartyListMaxH = 260f;
    private const float LogIndent     = 6f;
    private const float TrophySide    = 140f;

    private static readonly Vector4 CardAccent = EmporiumNeonTheme.CoinCollectorIndigo;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);
    private static readonly Vector4 GoldColour = new(1f, 0.84f, 0f, 1f);

    private readonly PluginConfiguration config;
    private readonly CoinCollectorService coinCollectorService;
    private readonly ChatQueueService chatQueue;
    private readonly AutoPayoutService autoPayoutService;
    private readonly CoinCollectorQueueService queueService;
    private readonly ISharedImmediateTexture? trophyTexture;
    private readonly ThemedCard card = new();

    private int wrongRollValue;
    private int wrongRollMax;
    private int wrongRollExpected;
    private string wrongRollPlayer = string.Empty;

    public CoinCollectorGameTab(PluginConfiguration config, CoinCollectorService coinCollectorService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService, CoinCollectorQueueService queueService)
    {
        this.config               = config;
        this.coinCollectorService = coinCollectorService;
        this.chatQueue            = chatQueue;
        this.autoPayoutService    = autoPayoutService;
        this.queueService         = queueService;
        this.coinCollectorService.WrongRollDetected += OnWrongRollDetected;
        this.coinCollectorService.RollAwaitingNext  += OnValidRoll;
        this.coinCollectorService.TurnCompleted     += OnTurnCompleted;
        var path = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "trophy.png");
        if (File.Exists(path))
            this.trophyTexture = MiniGamesEmporium.TextureProvider.GetFromFile(path);
    }

    public void Draw(bool skipLeadingSpacing = false, float reserveBottom = 0f, Action? drawBottomPanel = null)
    {
        if (!skipLeadingSpacing) ImGui.Spacing();
        var session = this.coinCollectorService.GetActiveSession();
        if (session == null || !CoinCollectorGameIds.Matches(session.GameName)) return;
        var fullH = MathF.Max(100f, ImGui.GetContentRegionAvail().Y);
        using var split = ImRaii.Table("##CCSplit_v2",
            CollapsiblePanels.SideColumnCount(PanelKeys.CoinCollectorSide),
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1f, fullH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##CCGameCol", ImGuiTableColumnFlags.WidthStretch);
        CollapsiblePanels.SetupSideColumns(PanelKeys.CoinCollectorSide, "##CCLeaderboard", RightPaneW * ImGuiHelpers.GlobalScale);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        var cellH = ImGui.GetContentRegionAvail().Y;
        var colTopY = ImGui.GetCursorPosY();
        DrawGamePane(session, MathF.Max(100f, cellH - reserveBottom));
        if (drawBottomPanel != null)
        {
            var targetY = colTopY + cellH - reserveBottom;
            if (targetY > ImGui.GetCursorPosY())
                ImGui.SetCursorPosY(targetY);
            drawBottomPanel();
        }

        ImGui.TableSetColumnIndex(1);
        var showSidePane = CollapsiblePanels.DrawSideTag(
            PanelKeys.CoinCollectorSide, "##CCLeaderboardTag", CardAccent, "the leaderboard");
        if (!showSidePane) return;
        ImGui.TableSetColumnIndex(2);
        DrawLeaderboardPane(ImGui.GetContentRegionAvail().Y);
    }

    private void DrawGamePane(ActiveSession session, float height)
    {
        using var pane = ImRaii.Child("##CCGamePane", new Vector2(-1f, height), false);
        if (!pane.Success) return;
        DrawActiveSessionView(session);
    }

    public void DrawSessionActionButtons()
    {
        var row = new ShoutButtonRow();
        using (UIHelper.PushBlueButtonColours())
            if (row.Button(FontAwesomeIcon.Scroll, "Send Rules", "##CCSendRules"))
                AnnounceRules.Execute(this.config, this.chatQueue);
        using (UIHelper.PushOrangeButtonColours())
            if (row.Button(FontAwesomeIcon.Bullhorn, "Advertise", "##CCAdvertise"))
                Advertise.Execute(this.config, this.chatQueue);
        DrawStripFinishButton(row);
    }

    private void DrawStripFinishButton(ShoutButtonRow row)
    {
        if (!CollapsiblePanels.IsCollapsed(PanelKeys.CoinCollectorSide)) return;
        if (this.coinCollectorService.IsSessionFinished()) return;

        var canFinish = this.config.CoinCollector.SessionLeaderboard.Count > 0;
        using (ImRaii.Disabled(!canFinish))
        using (UIHelper.PushGreenButtonColours())
        {
            if (row.Button(FontAwesomeIcon.Trophy, "Finish Game", "##CCStripFinishGame"))
                this.coinCollectorService.FinishSession();
        }
        if (!canFinish && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("No players have finished a turn yet.");
    }

    private void DrawLeaderboardPane(float height)
    {
        using var pane = ImRaii.Child("##CCLeaderboardPane", new Vector2(-1f, height), false, ImGuiWindowFlags.NoScrollbar);
        if (!pane.Success) return;

        var finished = this.coinCollectorService.IsSessionFinished();
        var board    = this.config.CoinCollector.SessionLeaderboard;

        var showFinish = !finished;

        var frameH        = ImGui.GetFrameHeight();
        var sp            = ImGui.GetStyle().ItemSpacing.Y;
        var btnRows       = showFinish ? 1 : 0;
        var ctrlH         = btnRows > 0 ? frameH * btnRows + sp * (btnRows + 1) + 4f : 0f;
        var boardSectionH = MathF.Max(40f, height - ctrlH - sp);

        {
            using var boardSection = ImRaii.Child("##CCBoardSection", new Vector2(-1f, boardSectionH), false);
            if (boardSection.Success)
            {
                UIHelper.CentreText("Leaderboard", EmporiumNeonTheme.CoinCollectorIndigo);
                ImGui.Separator();
                if (board.Count == 0)
                    ImGui.TextDisabled("No one has played yet.");
                else
                    DrawLeaderboardTable(BuildLeaderboardRows());
            }
        }

        if (btnRows > 0)
            DrawRightPaneControls(board.Count > 0);
    }

    private static void DrawLeaderboardTable(LeaderboardRow[] rows)
    {
        using var tbl = ImRaii.Table("##CCLeaderboardTbl", 4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(-1f, -1f));
        if (!tbl.Success) return;
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#",      ImGuiTableColumnFlags.WidthFixed,  26f);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Best",   ImGuiTableColumnFlags.WidthFixed,  40f);
        ImGui.TableSetupColumn("Plays",  ImGuiTableColumnFlags.WidthFixed,  40f);
        ImGui.TableHeadersRow();
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var col = row.IsEffectiveWinner ? GoldColour : EmporiumNeonTheme.NeonCyan;
            ImGui.TableNextRow();
            if (row.IsEffectiveWinner)
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.14f, 0.10f, 0.30f, 1f)));
            ImGui.TableSetColumnIndex(0); ImGui.TextColored(col, (i + 1).ToString());
            ImGui.TableSetColumnIndex(1); ImGui.TextColored(col, row.PlayerName);
            ImGui.TableSetColumnIndex(2); ImGui.TextColored(col, row.BestScore.ToString());
            ImGui.TableSetColumnIndex(3); ImGui.TextColored(col, row.TimesPlayed.ToString());
        }
    }

    private LeaderboardRow[] BuildLeaderboardRows() =>
        this.coinCollectorService.BuildStandings()
            .Select(r => new LeaderboardRow(r.PlayerName, r.BestScore, r.TimesPlayed, r.FirstBestAt, r.IsEffectiveWinner))
            .ToArray();

    private void DrawActiveSessionView(ActiveSession session)
    {
        if (this.coinCollectorService.IsSessionFinished())
        {
            DrawSessionWinnerScreen();
            return;
        }

        DrawLivePhase(session);
    }

    private void DrawLivePhase(ActiveSession session)
    {
        if (!session.PlayerSet)
        {
            this.card.Draw("##CCPartyCard", "Select Player from Party", CardAccent, CardTitle, DrawPartyMemberList);
            return;
        }

        var turn = this.coinCollectorService.GetActiveTurn();

        if (turn != null && turn.IsGameOver)
        {
            this.card.Draw("##CCTurnOverCard", "Turn Complete", CardAccent, CardTitle, () => DrawGameOverBody(session, turn));
            return;
        }

        this.card.Draw("##CCPlayerCard", "Player", CardAccent, CardTitle, () => DrawPlayerBody(session));

        if (!session.PaymentVerified)
        {
            DrawTakeBetPhase(session);
            return;
        }

        DrawActionCards(session, turn);
    }

    private void DrawPartyMemberList()
    {
        var roster = this.queueService.GetRoster();
        if (roster.Count == 0)
        {
            UIHelper.CentreTextDisabled("No other party members found. Invite the player to your party first.");
            return;
        }

        var rowH   = ImGui.GetFrameHeight() + ImGui.GetStyle().CellPadding.Y * 2f;
        var height = MathF.Min(PartyListMaxH * ImGuiHelpers.GlobalScale, rowH * (roster.Count + 1));
        var nextUp = FirstUnplayedIndex(roster);

        using var table = ImRaii.Table("##CCPartyList", 4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(-1f, height));
        if (!table.Success) return;
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#",          ImGuiTableColumnFlags.WidthFixed, 26f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Player",     ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Played",     ImGuiTableColumnFlags.WidthFixed, 74f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##CCSelBtn", ImGuiTableColumnFlags.WidthFixed, 90f * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        for (var i = 0; i < roster.Count; i++)
            DrawRosterRow(roster[i], i + 1, i == nextUp);
    }

    private static int FirstUnplayedIndex(IReadOnlyList<CoinCollectorQueueEntry> roster)
    {
        for (var i = 0; i < roster.Count; i++)
        {
            if (roster[i].TurnsTaken == 0) return i;
        }
        return -1;
    }

    private void DrawRosterRow(CoinCollectorQueueEntry entry, int position, bool isNextUp)
    {
        var played = entry.TurnsTaken > 0;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(position.ToString());

        ImGui.TableSetColumnIndex(1);
        if (played)
            ImGui.TextDisabled(entry.DisplayName);
        else if (isNextUp)
            ImGui.TextColored(EmporiumNeonTheme.SuccessMint, $"{entry.DisplayName}  (next up)");
        else
            ImGui.TextUnformatted(entry.DisplayName);

        ImGui.TableSetColumnIndex(2);
        if (played)
            ImGui.TextColored(EmporiumNeonTheme.NeonCyan, $"{entry.TurnsTaken}x  best {entry.BestCoins}");
        else
            ImGui.TextDisabled("-");

        ImGui.TableSetColumnIndex(3);
        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Select", $"##CCSel_{entry.PlayerName}"))
            this.coinCollectorService.SetPlayer(entry.PlayerName, entry.PlayerWorld);
    }

    private void DrawPlayerBody(ActiveSession session)
    {
        UIHelper.CentreTextScaled(BuildDisplayName(session), EmporiumNeonTheme.SuccessMint, 1.4f);
        var attempts = this.coinCollectorService.GetAttempts();
        if (session.PaymentVerified && attempts.AttemptsPurchased > 1)
            UIHelper.CentreText($"Attempt {attempts.AttemptsUsed} of {attempts.AttemptsPurchased}", EmporiumNeonTheme.NeonCyan);
        ImGui.Spacing();

        using (UIHelper.PushRedButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.ExclamationTriangle, "End Turn Early", "##CCEndEarlyBtn")
                && ImGui.GetIO().KeyCtrl)
                this.coinCollectorService.EndCurrentTurn();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hold CTRL + click to end this player's turn");
    }

    private void DrawTakeBetPhase(ActiveSession session)
    {
        var pairHeight = this.card.MatchedHeight("##CCTakeBetCard", "##CCBuyerCard");
        using (var split = ImRaii.Table("##CCTakeBetSplit", 2, ImGuiTableFlags.None, new Vector2(-1f, 0f)))
        {
            if (split.Success)
            {
                ImGui.TableSetupColumn("##CCTakeBetLeft",  ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##CCTakeBetRight", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                this.card.Draw("##CCTakeBetCard", "Take Bet", CardAccent, CardTitle, pairHeight, () => DrawPrimaryBetActions(session));
                ImGui.TableSetColumnIndex(1);
                this.card.Draw("##CCBuyerCard", "Paying for Another Player", CardAccent, CardTitle, pairHeight, () => DrawBuyerSection(session));
            }
        }

        this.card.Draw("##CCBeginCard", "Begin Game", CardAccent, CardTitle, () => DrawBeginGameBody(session));
    }

    private void DrawPrimaryBetActions(ActiveSession session)
    {
        var row = new ShoutButtonRow();
        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Request Gil"),
            (FontAwesomeIcon.Coins, "Trade"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (row.Button(FontAwesomeIcon.CommentDots, "Request Gil", "##CCRequestGil"))
                RequestEntryFee.Execute(BuildDisplayName(session), this.config, this.chatQueue);
        }

        using (UIHelper.PushAmberButtonColours())
        {
            if (row.Button(FontAwesomeIcon.Coins, "Trade", "##CCTradeBtn"))
                SendTradeRequest.Execute(session.PlayerName, this.chatQueue);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        UIHelper.CentreText("No payment required", EmporiumNeonTheme.WarnAmber);
        UIHelper.CentreTextDisabled("Gil will not be added to pot");
        ImGui.Spacing();

        using (UIHelper.PushRedButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.UserShield, "Skip Payment", "##CCBypassPayment"))
                this.coinCollectorService.StartGame();
        }
    }

    private void DrawBuyerSection(ActiveSession session)
    {
        var buyer = this.coinCollectorService.GetBuyer();
        if (!string.IsNullOrEmpty(buyer))
        {
            DrawAssignedBuyer(session, buyer);
            return;
        }

        var (charName, worldName) = GetCurrentTarget();
        if (string.IsNullOrEmpty(charName))
        {
            UIHelper.CentreTextDisabled("Target a player in-game to set them as the buyer.");
            return;
        }

        var style        = ImGui.GetStyle();
        var targetedRowW = ImGui.CalcTextSize("Targeted:").X + style.ItemSpacing.X + ImGui.CalcTextSize(charName).X;
        UIHelper.CentreNext(targetedRowW);
        ImGui.TextDisabled("Targeted:");
        ImGui.SameLine();
        ImGui.TextUnformatted(charName);
        ImGui.Spacing();

        using (UIHelper.PushGreenButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.UserCheck, "Set as Buyer", "##CCSetBuyer"))
            {
                var full = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
                this.coinCollectorService.SetBuyer(full);
            }
        }
    }

    private void DrawAssignedBuyer(ActiveSession session, string buyer)
    {
        var style  = ImGui.GetStyle();
        var clearW = UIHelper.CalcButtonSize(FontAwesomeIcon.Times, "Clear").X;
        var rowW   = ImGui.CalcTextSize("Buyer:").X + style.ItemSpacing.X + ImGui.CalcTextSize(buyer).X + style.ItemSpacing.X + clearW;
        UIHelper.CentreNext(rowW);
        ImGui.TextDisabled("Buyer:");
        ImGui.SameLine();
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, buyer);
        ImGui.SameLine();
        using (UIHelper.PushRedButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Clear", "##CCClearBuyer"))
                this.coinCollectorService.ClearBuyer();
        }
        ImGui.Spacing();

        var row = new ShoutButtonRow();
        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Request Gil (Buyer)"),
            (FontAwesomeIcon.Coins, "Trade (Buyer)"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (row.Button(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", "##CCBuyerRequestGil"))
                RequestEntryFeeBuyer.Execute(buyer, session.PlayerName, this.config, this.chatQueue);
        }

        using (UIHelper.PushAmberButtonColours())
        {
            if (row.Button(FontAwesomeIcon.Coins, "Trade (Buyer)", "##CCBuyerTrade"))
                SendTradeRequest.Execute(buyer, this.chatQueue);
        }
    }

    private void DrawBeginGameBody(ActiveSession session)
    {
        UIHelper.CentreTextDisabled(session.AmountTraded > 0
            ? $"{session.AmountTraded:N0} Gil received"
            : "No trade recorded yet.");
        var purchased = this.coinCollectorService.GetAttempts().AttemptsPurchased;
        if (purchased > 1)
            UIHelper.CentreText($"{purchased} entries paid - {purchased} turns", EmporiumNeonTheme.SuccessMint);
        ImGui.Spacing();

        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Play, "Begin Game", "##CCBeginGame"))
            this.coinCollectorService.StartGame();
    }

    private void DrawActionCards(ActiveSession session, CoinCollectorTurnState? turn)
    {
        this.card.Draw("##CCRollCard", "Current Roll", CardAccent, CardTitle, () => DrawRollBody(session, turn));

        var logH = MathF.Max(MinLogHeight, ImGui.GetContentRegionAvail().Y - ThemedCard.ChromeHeight());
        this.card.Draw("##CCLogCard", "Game Log", CardAccent, CardTitle, logH, DrawGameLogBody);
    }

    private void DrawRollBody(ActiveSession session, CoinCollectorTurnState? turn)
    {
        var coins   = turn?.CoinsCollected ?? 0;
        var nextMax = this.coinCollectorService.GetNextRollCommandMax();

        DrawWrongRollWarning(session);

        UIHelper.CentreValueRowScaled(
            "##CCNumInfo",
            turn?.RollLog.Count > 0 ? turn.RollLog[^1].ToString() : "--",
            EmporiumNeonTheme.CoinCollectorIndigo,
            2.8f,
            turn?.RollMaxLog.Count > 0 ? $"Rolled out of: {turn.RollMaxLog[^1]}" : "Rolled out of: -",
            $"Coins: {coins}",
            EmporiumNeonTheme.SuccessMint);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var nextCommand = nextMax > 0 ? $"/dice {nextMax}" : "/dice";
        UIHelper.CentreText($"Waiting on the player to roll {nextCommand}", EmporiumNeonTheme.NeonCyan);
        ImGui.Spacing();

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Dice, "Ask to Roll", "##CCAskRoll"))
                AnnounceAskRoll.Execute(BuildDisplayName(session), nextMax, coins, this.config, this.chatQueue);
        }
    }

    private void DrawWrongRollWarning(ActiveSession session)
    {
        if (this.wrongRollMax <= 0) return;
        if (!this.wrongRollPlayer.Equals(session.PlayerName, StringComparison.OrdinalIgnoreCase))
        {
            ClearWrongRoll();
            return;
        }

        var expected = this.wrongRollExpected > 0 ? $"/dice {this.wrongRollExpected}" : "/dice";
        UIHelper.CentreText($"Wrong number rolled - {this.wrongRollValue} out of {this.wrongRollMax}", EmporiumNeonTheme.Bar777Red);
        UIHelper.CentreTextDisabled($"That roll did not count. They need to roll {expected}.");
        ImGui.Spacing();

        var row = new ShoutButtonRow();
        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Send Correction"),
            (FontAwesomeIcon.Times, "Dismiss"));
        using (UIHelper.PushOrangeButtonColours())
        {
            if (row.Button(FontAwesomeIcon.CommentDots, "Send Correction", "##CCSendWrongRoll"))
            {
                AnnounceWrongRoll.Execute(BuildDisplayName(session), this.wrongRollMax, this.wrongRollExpected, this.config, this.chatQueue);
                ClearWrongRoll();
            }
        }
        using (UIHelper.PushRedButtonColours())
        {
            if (row.Button(FontAwesomeIcon.Times, "Dismiss", "##CCDismissWrongRoll"))
                ClearWrongRoll();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    private void DrawGameLogBody()
    {
        using var scroll = ImRaii.Child("##CCInlineLog", new Vector2(-1f, -1f), false);
        if (!scroll.Success) return;
        var indent = LogIndent * ImGuiHelpers.GlobalScale;
        ImGui.Indent(indent);
        var log = this.coinCollectorService.GetGameLog();
        if (log.Count == 0)
        {
            ImGui.TextDisabled("No events yet.");
        }
        else
        {
            foreach (var entry in log)
                ImGui.TextUnformatted(entry);
            ImGui.SetScrollHereY(1.0f);
        }
        ImGui.Unindent(indent);
    }

    private void DrawRightPaneControls(bool canFinish)
    {
        ImGui.Separator();
        ImGui.Spacing();

        using (ImRaii.Disabled(!canFinish))
        using (UIHelper.PushGreenButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Trophy, "Finish Game", "##CCFinishGame"))
                this.coinCollectorService.FinishSession();
        }
        if (!canFinish && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("No players have finished a turn yet.");
    }

    private void DrawGameOverBody(ActiveSession session, CoinCollectorTurnState turn)
    {
        var attempts = this.coinCollectorService.GetAttempts();

        UIHelper.CentreTextScaled(BuildDisplayName(session), EmporiumNeonTheme.WarnAmber, 1.6f);
        ImGui.Spacing();
        UIHelper.CentreText($"Coins Collected: {turn.CoinsCollected}", EmporiumNeonTheme.NeonCyan);
        if (attempts.AttemptsPurchased > 1)
            UIHelper.CentreTextDisabled($"Attempt {attempts.AttemptsUsed} of {attempts.AttemptsPurchased}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var isLeading     = this.coinCollectorService.IsCurrentlyLeading(turn.CoinsCollected);
        var target        = this.coinCollectorService.GetLeadTarget(turn.CoinsCollected);
        var announceIcon  = isLeading ? FontAwesomeIcon.Star : FontAwesomeIcon.Medal;
        var announceLabel = isLeading ? "Announce Lead" : "Announce Score";
        using (isLeading ? UIHelper.PushGreenButtonColours() : UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.CentredIconTextButton(announceIcon, announceLabel, "##CCAnnounceGameOver"))
                AnnounceCoinCollectorLoss.ExecuteLeaderboardAnnounce(
                    BuildDisplayName(session), turn.CoinsCollected, isLeading, target,
                    this.config, this.chatQueue);
        }

        ImGui.Spacing();

        var hasMore   = this.coinCollectorService.HasAttemptsRemaining();
        var nextIcon  = hasMore ? FontAwesomeIcon.Redo : FontAwesomeIcon.FlagCheckered;
        var nextLabel = hasMore ? $"Next Attempt ({attempts.Remaining} left)" : "End Turn";
        using (UIHelper.PushGreenButtonColours())
        {
            if (UIHelper.CentredIconTextButton(nextIcon, nextLabel, "##CCEndTurnBtn"))
                this.coinCollectorService.AdvanceAfterTurn();
        }
    }

    private void DrawSessionWinnerScreen()
    {
        var winners  = this.coinCollectorService.GetSessionWinners();
        var totalPot = this.coinCollectorService.GetTotalPot();
        var share    = this.coinCollectorService.GetSessionWinnerShare();

        this.card.Draw("##CCSessionDoneCard", "Session Complete", CardAccent, CardTitle,
            () => DrawSessionSummaryBody(winners.Count, totalPot, share));

        foreach (var winner in winners)
        {
            var name  = winner.Name;
            var coins = winner.Coins;
            this.card.Draw($"##CCWinnerCard_{name}", name, CardAccent, GoldColour,
                () => DrawWinnerPayoutBody(name, coins, totalPot, share));
        }
    }

    private void DrawSessionSummaryBody(int winnerCount, long totalPot, long share)
    {
        if (winnerCount > 0)
        {
            DrawTrophy();
            ImGui.Spacing();
        }

        UIHelper.CentreTextScaled($"Total Pot: {totalPot:N0} Gil", GoldColour, 1.3f);

        if (winnerCount > 1)
            UIHelper.CentreText($"{winnerCount} winners - {share:N0} Gil each", EmporiumNeonTheme.NeonCyan);

        ImGui.Spacing();

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Undo, "Resume Session", "##CCResumeSession"))
                this.coinCollectorService.ResumeFinishedSession();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Undo finishing and keep playing - the leaderboard is kept.");

        if (winnerCount == 0)
        {
            ImGui.Spacing();
            UIHelper.CentreTextDisabled("No winners to pay out.");
        }
    }

    private void DrawWinnerPayoutBody(string displayName, int coins, long totalPot, long share)
    {
        var baseName  = PlayerInfoService.StripWorld(displayName);
        var paid      = this.coinCollectorService.GetWinnerPaid(displayName);
        var remaining = Math.Max(0L, share - paid);

        UIHelper.CentreText($"{coins} Coins Collected!", GoldColour);
        ImGui.Spacing();

        DrawPayoutFigures(share, paid, remaining);
        ImGui.Spacing();

        using (UIHelper.PushGoldButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Bullhorn, "Announce Winner", $"##CCAnnounceWinner_{displayName}"))
                AnnounceCoinCollectorWin.Execute(displayName, coins, totalPot, share, this.config, this.chatQueue);
        }

        ImGui.Spacing();

        var payoutRunning = this.autoPayoutService.IsRunningFor(baseName);
        var payoutIcon    = payoutRunning ? FontAwesomeIcon.Stop : FontAwesomeIcon.MoneyBillWave;
        var payoutLabel   = payoutRunning ? "Stop Auto Payout" : "Auto Payout";
        var row = new ShoutButtonRow();
        UIHelper.CentreNextButtonRow((FontAwesomeIcon.Coins, "Trade Winner"), (payoutIcon, payoutLabel));

        using (UIHelper.PushAmberButtonColours())
        {
            if (row.Button(FontAwesomeIcon.Coins, "Trade Winner", $"##CCWinnerTrade_{displayName}"))
                SendTradeRequest.Execute(baseName, this.chatQueue);
        }

        DrawWinnerAutoPayoutButton(row, displayName, baseName, remaining);
        ImGui.Spacing();
        DrawPayoutProgressBar(share, paid);
    }

    private void DrawTrophy()
    {
        var tex = this.trophyTexture?.GetWrapOrDefault();
        if (tex == null) return;
        var side = TrophySide * ImGuiHelpers.GlobalScale;
        UIHelper.CentreNext(side);
        ImGui.Image(tex.Handle, new Vector2(side, side));
    }

    private static void DrawPayoutFigures(long share, long paid, long remaining)
    {
        var labelColW = MathF.Max(ImGui.CalcTextSize("Share:").X, MathF.Max(ImGui.CalcTextSize("Traded:").X, ImGui.CalcTextSize("Remaining:").X));
        var valueColW = MathF.Max(ImGui.CalcTextSize($"{share:N0} Gil").X, MathF.Max(ImGui.CalcTextSize($"{paid:N0} Gil").X, ImGui.CalcTextSize($"{remaining:N0} Gil").X));
        var sp        = ImGui.GetStyle().ItemSpacing.X;
        var blockW    = labelColW + sp + valueColW;
        var rowX      = ImGui.GetCursorPosX() + MathF.Max(0f, (ImGui.GetContentRegionAvail().X - blockW) * 0.5f);
        var valueX    = rowX + labelColW + sp;

        DrawFigureRow(rowX, valueX, "Share:",     $"{share:N0} Gil",     GoldColour);
        DrawFigureRow(rowX, valueX, "Traded:",    $"{paid:N0} Gil",      EmporiumNeonTheme.SuccessMint);
        DrawFigureRow(rowX, valueX, "Remaining:", $"{remaining:N0} Gil", EmporiumNeonTheme.WarnAmber);
    }

    private static void DrawFigureRow(float rowX, float valueX, string label, string value, Vector4 colour)
    {
        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(colour, label);
        ImGui.SameLine(valueX);
        ImGui.TextColored(colour, value);
    }

    private void DrawWinnerAutoPayoutButton(ShoutButtonRow row, string displayName, string baseName, long remaining)
    {
        if (this.autoPayoutService.IsRunningFor(baseName))
        {
            using var red = UIHelper.PushRedButtonColours();
            if (row.Button(FontAwesomeIcon.Stop, "Stop Auto Payout", $"##CCStopAutoPayout_{displayName}"))
                this.autoPayoutService.Stop();
            return;
        }

        using var dis   = ImRaii.Disabled(remaining <= 0 || this.autoPayoutService.IsRunning);
        using var green = UIHelper.PushGreenButtonColours();
        if (row.Button(FontAwesomeIcon.MoneyBillWave, "Auto Payout", $"##CCAutoPayout_{displayName}"))
        {
            this.autoPayoutService.Start(
                baseName,
                () => this.coinCollectorService.GetWinnerRemaining(displayName),
                () => this.coinCollectorService.IsSessionFinished());
        }
    }

    private static void DrawPayoutProgressBar(long share, long paid)
    {
        var progress = share > 0 ? MathF.Min(1f, (float)paid / share) : 1f;
        ImGui.ProgressBar(progress, new Vector2(-1f, ImGui.GetFrameHeight()), $"{progress * 100f:F0}% paid out");
    }

    private static (string CharName, string WorldName) GetCurrentTarget()
    {
        var pc = MiniGamesEmporium.TargetManager.Target as Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter;
        if (pc == null) return (string.Empty, string.Empty);
        return (pc.Name.TextValue, pc.HomeWorld.Value.Name.ToString());
    }

    private static string BuildDisplayName(ActiveSession session) =>
        string.IsNullOrEmpty(session.PlayerWorld)
            ? session.PlayerName
            : $"{session.PlayerName}@{session.PlayerWorld}";

    private void OnWrongRollDetected(int rollValue, int rollMax, int expectedMax)
    {
        this.wrongRollValue    = rollValue;
        this.wrongRollMax      = rollMax;
        this.wrongRollExpected = expectedMax;
        this.wrongRollPlayer   = this.coinCollectorService.GetActiveSession()?.PlayerName ?? string.Empty;
    }

    private void OnValidRoll(int rollValue, int coins) => ClearWrongRoll();

    private void OnTurnCompleted(string playerName, int coins) => ClearWrongRoll();

    private void ClearWrongRoll()
    {
        this.wrongRollValue    = 0;
        this.wrongRollMax      = 0;
        this.wrongRollExpected = 0;
        this.wrongRollPlayer   = string.Empty;
    }

    public void Dispose()
    {
        this.coinCollectorService.WrongRollDetected -= OnWrongRollDetected;
        this.coinCollectorService.RollAwaitingNext  -= OnValidRoll;
        this.coinCollectorService.TurnCompleted     -= OnTurnCompleted;
    }

    private readonly record struct LeaderboardRow(string PlayerName, int BestScore, int TimesPlayed, DateTime FirstBestAt, bool IsEffectiveWinner);
}
