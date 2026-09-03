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
using ECommons.DalamudServices;
using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Actions;
using MiniGamesEmporium.Games.HigherLower.Models;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.Utility;

/// <summary>Draws the active game view for Higher/Lower.</summary>

namespace MiniGamesEmporium.Games.HigherLower.UI.Tabs;
public sealed class HigherLowerGameTab
{
    private const float RightPaneW   = 250f;
    private const float MinLogHeight = 120f;
    private const float LogIndent    = 6f;
    private const float TrophySide   = 140f;

    private static readonly Vector4 CardAccent          = EmporiumNeonTheme.HigherLowerOrange;
    private static readonly Vector4 CardTitle           = EmporiumNeonTheme.Secondary(CardAccent);
    private static readonly Vector4 GoldColour          = new(1f, 0.84f, 0f, 1f);
    private static readonly Vector4 HigherColour        = new(0.20f, 0.85f, 0.35f, 1f);
    private static readonly Vector4 LowerColour         = new(0.25f, 0.55f, 1.00f, 1f);
    private static readonly Vector4 DetectedYellow      = new(1f, 0.85f, 0f, 1f);
    private static readonly Vector4 DetectedYellowHover = new(1f, 0.95f, 0.25f, 1f);
    private static readonly Vector4 DetectedYellowAct   = new(0.85f, 0.70f, 0f, 1f);
    private static readonly Vector4 DimGrey             = new(0.38f, 0.38f, 0.38f, 1f);
    private static readonly Vector4 DimGreyHover        = new(0.48f, 0.48f, 0.48f, 1f);
    private static readonly Vector4 DimGreyAct          = new(0.30f, 0.30f, 0.30f, 1f);

    private readonly PluginConfiguration config;
    private readonly HigherLowerService higherLowerService;
    private readonly ChatQueueService chatQueue;
    private readonly AutoPayoutService autoPayoutService;
    private readonly PlayerInfoService playerInfo;
    private readonly ISharedImmediateTexture? trophyTexture;
    private readonly ThemedCard card = new();

    public HigherLowerGameTab(PluginConfiguration config, HigherLowerService higherLowerService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService, PlayerInfoService playerInfo)
    {
        this.config             = config;
        this.higherLowerService = higherLowerService;
        this.chatQueue          = chatQueue;
        this.autoPayoutService  = autoPayoutService;
        this.playerInfo         = playerInfo;
        var path = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "trophy.png");
        if (File.Exists(path))
            this.trophyTexture = MiniGamesEmporium.TextureProvider.GetFromFile(path);
    }

    public void Draw(bool skipLeadingSpacing = false, float reserveBottom = 0f, Action? drawBottomPanel = null)
    {
        if (!skipLeadingSpacing) ImGui.Spacing();
        var session = this.higherLowerService.GetActiveSession();
        if (session == null || !HigherLowerGameIds.Matches(session.GameName)) return;
        var fullH = MathF.Max(100f, ImGui.GetContentRegionAvail().Y);
        using var split = ImRaii.Table("##HLSplit_v2",
            CollapsiblePanels.SideColumnCount(PanelKeys.HigherLowerSide),
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1f, fullH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##HLGameCol", ImGuiTableColumnFlags.WidthStretch);
        CollapsiblePanels.SetupSideColumns(PanelKeys.HigherLowerSide, "##HLLeaderboard", RightPaneW * ImGuiHelpers.GlobalScale);
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
        if (!CollapsiblePanels.DrawSideTag(PanelKeys.HigherLowerSide, "##HLLeaderboardTag", CardAccent, "the leaderboard"))
            return;
        ImGui.TableSetColumnIndex(2);
        DrawLeaderboardPane(ImGui.GetContentRegionAvail().Y);
    }

    private void DrawGamePane(ActiveSession session, float height)
    {
        using var pane = ImRaii.Child("##HLGamePane", new Vector2(-1f, height), false);
        if (!pane.Success) return;
        DrawActiveSessionView(session);
    }

    public void DrawSessionActionButtons()
    {
        using (UIHelper.PushBlueButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Scroll, "Send Rules", "##HLSendRules"))
                AnnounceRules.Execute(this.config, this.chatQueue);
        ImGui.SameLine();
        using (UIHelper.PushOrangeButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Advertise", "##HLAdvertise"))
                Advertise.Execute(this.config, this.chatQueue);
    }

    private void DrawLeaderboardPane(float height)
    {
        using var pane = ImRaii.Child("##HLLeaderboardPane", new Vector2(-1f, height), false, ImGuiWindowFlags.NoScrollbar);
        if (!pane.Success) return;

        var finished = this.higherLowerService.IsSessionFinished();
        var board    = this.config.HigherLower.SessionLeaderboard;

        var showFinish = !finished;

        var frameH        = ImGui.GetFrameHeight();
        var sp            = ImGui.GetStyle().ItemSpacing.Y;
        var btnRows       = showFinish ? 1 : 0;
        var ctrlH         = btnRows > 0 ? frameH * btnRows + sp * (btnRows + 1) + 4f : 0f;
        var boardSectionH = MathF.Max(40f, height - ctrlH - sp);

        {
            using var boardSection = ImRaii.Child("##HLBoardSection", new Vector2(-1f, boardSectionH), false);
            if (boardSection.Success)
            {
                UIHelper.CentreText("Leaderboard", EmporiumNeonTheme.HigherLowerOrange);
                ImGui.Separator();
                if (board.Count == 0)
                    ImGui.TextDisabled("No one has played yet.");
                else
                    DrawLeaderboardTable(BuildLeaderboardRows(board));
            }
        }

        if (btnRows > 0)
            DrawRightPaneControls(board.Count > 0);
    }

    private static void DrawLeaderboardTable(LeaderboardRow[] rows)
    {
        using var tbl = ImRaii.Table("##HLLeaderboardTbl", 4,
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
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.25f, 0.20f, 0.02f, 1f)));
            ImGui.TableSetColumnIndex(0); ImGui.TextColored(col, (i + 1).ToString());
            ImGui.TableSetColumnIndex(1); ImGui.TextColored(col, row.PlayerName);
            ImGui.TableSetColumnIndex(2); ImGui.TextColored(col, row.BestScore.ToString());
            ImGui.TableSetColumnIndex(3); ImGui.TextColored(col, row.TimesPlayed.ToString());
        }
    }

    private LeaderboardRow[] BuildLeaderboardRows(List<HigherLowerLeaderboardEntry> board)
    {
        var groups = new Dictionary<string, (int Best, int Plays, DateTime FirstBestAt, bool HasWin)>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in board)
        {
            if (!groups.TryGetValue(e.PlayerName, out var g))
            {
                groups[e.PlayerName] = (e.RoundsCorrect, 1, e.PlayedAt, e.IsWinner);
                continue;
            }
            var firstBestAt = e.RoundsCorrect > g.Best ? e.PlayedAt
                            : e.RoundsCorrect == g.Best && e.PlayedAt < g.FirstBestAt ? e.PlayedAt
                            : g.FirstBestAt;
            groups[e.PlayerName] = (Math.Max(g.Best, e.RoundsCorrect), g.Plays + 1, firstBestAt, g.HasWin || e.IsWinner);
        }
        var allowMultiple = this.config.HigherLower.AllowMultipleWinners;
        string? singleLeader = null;
        if (!allowMultiple)
            singleLeader = groups.OrderByDescending(kv => kv.Value.Best).ThenBy(kv => kv.Value.FirstBestAt).First().Key;
        return groups
            .Select(kv => new LeaderboardRow(
                kv.Key,
                kv.Value.Best,
                kv.Value.Plays,
                kv.Value.FirstBestAt,
                allowMultiple ? kv.Value.HasWin : kv.Key.Equals(singleLeader, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => r.IsEffectiveWinner)
            .ThenByDescending(r => r.BestScore)
            .ThenBy(r => r.FirstBestAt)
            .ToArray();
    }

    private void DrawActiveSessionView(ActiveSession session)
    {
        if (this.higherLowerService.IsSessionFinished())
        {
            DrawSessionWinnerScreen();
            return;
        }

        if (!session.PlayerSet)
        {
            this.card.Draw("##HLPartyCard", "Select Player from Party", CardAccent, CardTitle, DrawPartyMemberList);
            return;
        }

        var turn = this.higherLowerService.GetActiveTurn();

        if (turn != null && turn.IsGameOver)
        {
            this.card.Draw("##HLTurnOverCard", "Turn Complete", CardAccent, CardTitle, () => DrawGameOverBody(session, turn));
            return;
        }

        this.card.Draw("##HLPlayerCard", "Player", CardAccent, CardTitle, () => DrawPlayerBody(session));

        if (!session.PaymentVerified)
        {
            DrawTakeBetPhase(session, turn);
            return;
        }

        DrawActionCards(session, turn);
    }

    private void DrawPartyMemberList()
    {
        var members = GetPartyMembers();
        if (members.Count == 0)
        {
            UIHelper.CentreTextDisabled("No other party members found. Invite the player to your party first.");
            return;
        }

        using var table = ImRaii.Table("##HLPartyList", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(-1f, 0f));
        if (!table.Success) return;
        ImGui.TableSetupColumn("Player",     ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##HLSelBtn", ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableHeadersRow();

        foreach (var (charName, worldName, displayName) in members)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(displayName);
            ImGui.TableSetColumnIndex(1);
            using var green = UIHelper.PushGreenButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Select", $"##HLSel_{charName}"))
                this.higherLowerService.SetPlayer(charName, worldName);
        }
    }

    private List<(string CharName, string WorldName, string DisplayName)> GetPartyMembers()
    {
        var result = new List<(string, string, string)>();
        foreach (var member in Svc.Party)
        {
            var name = member.Name.TextValue;
            if (string.IsNullOrEmpty(name)) continue;
            if (this.playerInfo.IsHost(name)) continue;
            var world   = member.World.ValueNullable?.Name.ToString() ?? string.Empty;
            var display = string.IsNullOrEmpty(world) ? name : $"{name}@{world}";
            result.Add((name, world, display));
        }
        return result;
    }

    private void DrawPlayerBody(ActiveSession session)
    {
        UIHelper.CentreTextScaled(BuildDisplayName(session), EmporiumNeonTheme.SuccessMint, 1.4f);
        ImGui.Spacing();

        using (UIHelper.PushRedButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.ExclamationTriangle, "End Turn Early", "##HLEndEarlyBtn")
                && ImGui.GetIO().KeyCtrl)
                this.higherLowerService.EndCurrentTurn();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hold CTRL + click to end this player's turn");
    }

    private void DrawStatusLine(ActiveSession session, HigherLowerTurnState? turn) =>
        UIHelper.CentreTextScaled(
            GetStatusText(session, turn, this.config.HigherLower.DiceSides),
            GetStatusColour(session, turn),
            1.15f);

    private static string GetStatusText(ActiveSession session, HigherLowerTurnState? turn, int diceSides)
    {
        if (turn?.IsWinner   == true) return "WINNER!";
        if (turn?.IsGameOver == true) return $"Turn complete - {turn.RoundsCorrect} round(s) correct";
        if (!session.PaymentVerified)
            return session.AmountTraded > 0
                ? $"Received {session.AmountTraded:N0} Gil - click Begin Game"
                : "Awaiting payment";
        if (turn == null || turn.RollLog.Count == 0) return "Press Roll to start";
        var curr = turn.RollLog[^1];
        if (turn.GuessConfirmed)
        {
            var locked = turn.DetectedGuess == 1 ? "HIGHER" : "LOWER";
            return $"{locked} locked on {curr} - press Roll";
        }
        if (turn.DetectedGuess.HasValue)
            return $"You rolled a {curr} - {(turn.DetectedGuess == 1 ? "HIGHER" : "LOWER")} detected!";
        return $"You rolled a {curr} - Higher or Lower?";
    }

    private static Vector4 GetStatusColour(ActiveSession session, HigherLowerTurnState? turn)
    {
        if (turn?.IsWinner   == true) return EmporiumNeonTheme.WinGold;
        if (turn?.IsGameOver == true) return EmporiumNeonTheme.WarnAmber;
        if (!session.PaymentVerified) return EmporiumNeonTheme.WarnAmber;
        if (turn?.GuessConfirmed == true || turn?.DetectedGuess.HasValue == true)
            return turn!.DetectedGuess == 1 ? HigherColour : LowerColour;
        return EmporiumNeonTheme.NeonCyan;
    }

    private void DrawTakeBetPhase(ActiveSession session, HigherLowerTurnState? turn)
    {
        var pairHeight = this.card.MatchedHeight("##HLTakeBetCard", "##HLBuyerCard");
        using (var split = ImRaii.Table("##HLTakeBetSplit", 2, ImGuiTableFlags.None, new Vector2(-1f, 0f)))
        {
            if (split.Success)
            {
                ImGui.TableSetupColumn("##HLTakeBetLeft",  ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##HLTakeBetRight", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                this.card.Draw("##HLTakeBetCard", "Take Bet", CardAccent, CardTitle, pairHeight, () => DrawPrimaryBetActions(session));
                ImGui.TableSetColumnIndex(1);
                this.card.Draw("##HLBuyerCard", "Paying for Another Player", CardAccent, CardTitle, pairHeight, () => DrawBuyerSection(session));
            }
        }

        this.card.Draw("##HLBeginCard", "Begin Game", CardAccent, CardTitle, () => DrawBeginGameBody(session, turn));
    }

    private void DrawPrimaryBetActions(ActiveSession session)
    {
        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Request Gil"),
            (FontAwesomeIcon.Coins, "Trade"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil", "##HLRequestGil"))
                RequestEntryFee.Execute(BuildDisplayName(session), this.config, this.chatQueue);
        }

        ImGui.SameLine();

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", "##HLTradeBtn"))
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
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.UserShield, "Skip Payment", "##HLBypassPayment"))
                this.higherLowerService.StartGame();
        }
    }

    private void DrawBuyerSection(ActiveSession session)
    {
        var buyer = this.higherLowerService.GetBuyer();
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
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.UserCheck, "Set as Buyer", "##HLSetBuyer"))
            {
                var full = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
                this.higherLowerService.SetBuyer(full);
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
            if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Clear", "##HLClearBuyer"))
                this.higherLowerService.ClearBuyer();
        }
        ImGui.Spacing();

        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Request Gil (Buyer)"),
            (FontAwesomeIcon.Coins, "Trade (Buyer)"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", "##HLBuyerRequestGil"))
                RequestEntryFeeBuyer.Execute(buyer, session.PlayerName, this.config, this.chatQueue);
        }
        ImGui.SameLine();

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade (Buyer)", "##HLBuyerTrade"))
                SendTradeRequest.Execute(buyer, this.chatQueue);
        }
    }

    private void DrawBeginGameBody(ActiveSession session, HigherLowerTurnState? turn)
    {
        DrawStatusLine(session, turn);
        ImGui.Spacing();

        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Play, "Begin Game", "##HLBeginGame"))
            this.higherLowerService.StartGame();
    }

    private void DrawActionCards(ActiveSession session, HigherLowerTurnState? turn)
    {
        this.card.Draw("##HLRollCard", "Current Roll", CardAccent, CardTitle, () => DrawRollBody(session, turn));

        var logH = MathF.Max(MinLogHeight, ImGui.GetContentRegionAvail().Y - ThemedCard.ChromeHeight());
        this.card.Draw("##HLLogCard", "Game Log", CardAccent, CardTitle, logH, DrawGameLogBody);
    }

    private void DrawRollBody(ActiveSession session, HigherLowerTurnState? turn)
    {
        var hasRoll        = turn?.RollLog.Count > 0;
        var guessConfirmed = turn?.GuessConfirmed == true;

        UIHelper.CentreValueRowScaled(
            "##HLNumInfo",
            hasRoll ? turn!.RollLog[^1].ToString() : "--",
            EmporiumNeonTheme.HigherLowerOrange,
            2.8f,
            turn?.RollLog.Count >= 2 ? $"Previous: {turn!.RollLog[^2]}" : "Previous: -",
            $"Rounds Correct: {turn?.RoundsCorrect ?? 0}",
            EmporiumNeonTheme.SuccessMint);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawStatusLine(session, turn);
        ImGui.Spacing();

        if (!hasRoll)
            DrawRollButtons(session);
        else if (guessConfirmed)
            DrawRollAndCancelRow();
        else
            DrawGuessButtons(session, turn!);
    }

    private void DrawRollAndCancelRow()
    {
        var dice      = this.config.HigherLower.DiceSides;
        var rollLabel = $"Roll {dice}";

        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.Dice, rollLabel),
            (FontAwesomeIcon.Undo, "Cancel Guess"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Dice, rollLabel, "##HLRollBtn"))
                this.chatQueue.Enqueue($"/dice party {dice}");
        }

        ImGui.SameLine();

        using (UIHelper.PushRedButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Undo, "Cancel Guess", "##HLCancelGuess"))
                this.higherLowerService.ClearConfirmedGuess();
        }
    }

    private void DrawRollButtons(ActiveSession session)
    {
        var dice      = this.config.HigherLower.DiceSides;
        var rollLabel = $"Roll {dice}";

        var sp        = ImGui.GetStyle().ItemSpacing.X;
        var letsPlayW = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Let's Play Message").X;
        var rollW     = UIHelper.CalcButtonSize(FontAwesomeIcon.Dice, rollLabel).X;
        UIHelper.CentreNext(letsPlayW + rollW + sp * 2f);
        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Let's Play Message", "##HLLetsPlay"))
                AnnounceLetsPlay.Execute(BuildDisplayName(session), this.config, this.chatQueue);
        }
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Dice, rollLabel, "##HLRollBtn"))
                this.chatQueue.Enqueue($"/dice party {dice}");
        }
    }

    private void DrawGuessButtons(ActiveSession session, HigherLowerTurnState turn)
    {
        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Question, "Ask Guess", "##HLAskGuess"))
                AnnounceAskGuess.Execute(BuildDisplayName(session), turn.RollLog[^1], this.config, this.chatQueue);
        }

        ImGui.Spacing();

        var detectedGuess  = turn.DetectedGuess;
        var higherDetected = detectedGuess == 1;
        var lowerDetected  = detectedGuess == -1;
        var hasDetection   = detectedGuess.HasValue;
        var sp             = ImGui.GetStyle().ItemSpacing.X;
        var higherBtnW     = UIHelper.CalcButtonSize(FontAwesomeIcon.ArrowUp,   "Higher").X;
        var lowerBtnW      = UIHelper.CalcButtonSize(FontAwesomeIcon.ArrowDown, "Lower").X;
        UIHelper.CentreNext(higherBtnW + lowerBtnW + sp * 2f);

        if (higherDetected) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 1f));
        using (UIHelper.PushButtonColours(
            hasDetection ? (higherDetected ? DetectedYellow      : DimGrey)      : HigherColour,
            hasDetection ? (higherDetected ? DetectedYellowHover : DimGreyHover) : new Vector4(0.25f, 1f, 0.42f, 1f),
            hasDetection ? (higherDetected ? DetectedYellowAct   : DimGreyAct)   : new Vector4(0.18f, 0.72f, 0.32f, 1f)))
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowUp, "Higher", "##HLHigherBtn"))
                this.higherLowerService.RegisterGuess(isHigher: true);
        }
        if (higherDetected) ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        if (lowerDetected) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 1f));
        using (UIHelper.PushButtonColours(
            hasDetection ? (lowerDetected ? DetectedYellow      : DimGrey)      : LowerColour,
            hasDetection ? (lowerDetected ? DetectedYellowHover : DimGreyHover) : new Vector4(0.32f, 0.68f, 1f, 1f),
            hasDetection ? (lowerDetected ? DetectedYellowAct   : DimGreyAct)   : new Vector4(0.22f, 0.48f, 0.80f, 1f)))
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowDown, "Lower", "##HLLowerBtn"))
                this.higherLowerService.RegisterGuess(isHigher: false);
        }
        if (lowerDetected) ImGui.PopStyleColor();
    }

    private void DrawGameLogBody()
    {
        using var scroll = ImRaii.Child("##HLInlineLog", new Vector2(-1f, -1f), false);
        if (!scroll.Success) return;
        var indent = LogIndent * ImGuiHelpers.GlobalScale;
        ImGui.Indent(indent);
        var log = this.higherLowerService.GetGameLog();
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
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Trophy, "Finish Game", "##HLFinishGame"))
                this.higherLowerService.FinishSession();
        }
        if (!canFinish && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("No players have finished a turn yet.");
    }

    private void DrawGameOverBody(ActiveSession session, HigherLowerTurnState turn)
    {
        UIHelper.CentreTextScaled(BuildDisplayName(session), EmporiumNeonTheme.WarnAmber, 1.6f);
        ImGui.Spacing();
        UIHelper.CentreText($"Rounds Correct: {turn.RoundsCorrect}", EmporiumNeonTheme.NeonCyan);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var isLeading     = this.higherLowerService.IsCurrentlyLeading(turn.RoundsCorrect);
        var target        = this.higherLowerService.GetLeadTarget(turn.RoundsCorrect);
        var announceIcon  = isLeading ? FontAwesomeIcon.Star : FontAwesomeIcon.Medal;
        var announceLabel = isLeading ? "Announce Lead" : "Announce Score";
        using (isLeading ? UIHelper.PushGreenButtonColours() : UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.CentredIconTextButton(announceIcon, announceLabel, "##HLAnnounceGameOver"))
                AnnounceHigherLowerLoss.ExecuteLeaderboardAnnounce(
                    BuildDisplayName(session), turn.RoundsCorrect, isLeading, target,
                    this.config, this.chatQueue);
        }

        ImGui.Spacing();

        using (UIHelper.PushGreenButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.FlagCheckered, "End Turn", "##HLEndTurnBtn"))
                this.higherLowerService.EndCurrentTurn();
        }
    }

    private void DrawSessionWinnerScreen()
    {
        var winners  = this.higherLowerService.GetSessionWinners();
        var totalPot = this.higherLowerService.GetTotalPot();
        var share    = this.higherLowerService.GetSessionWinnerShare();

        this.card.Draw("##HLSessionDoneCard", "Session Complete", CardAccent, CardTitle,
            () => DrawSessionSummaryBody(winners.Count, totalPot, share));

        foreach (var winner in winners)
        {
            var name   = winner.Name;
            var rounds = winner.Rounds;
            this.card.Draw($"##HLWinnerCard_{name}", name, CardAccent, GoldColour,
                () => DrawWinnerPayoutBody(name, rounds, totalPot, share));
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
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Undo, "Resume Session", "##HLResumeSession"))
                this.higherLowerService.ResumeFinishedSession();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Undo finishing and keep playing - the leaderboard is kept.");

        if (winnerCount == 0)
        {
            ImGui.Spacing();
            UIHelper.CentreTextDisabled("No winners to pay out.");
        }
    }

    private void DrawWinnerPayoutBody(string displayName, int rounds, long totalPot, long share)
    {
        var baseName  = PlayerInfoService.StripWorld(displayName);
        var paid      = this.higherLowerService.GetWinnerPaid(displayName);
        var remaining = Math.Max(0L, share - paid);

        UIHelper.CentreText($"{rounds} Rounds Correct!", GoldColour);
        ImGui.Spacing();

        DrawPayoutFigures(share, paid, remaining);
        ImGui.Spacing();

        using (UIHelper.PushGoldButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Bullhorn, "Announce Winner", $"##HLAnnounceWinner_{displayName}"))
                AnnounceHigherLowerWin.Execute(displayName, rounds, totalPot, share, this.config, this.chatQueue);
        }

        ImGui.Spacing();

        var payoutRunning = this.autoPayoutService.IsRunningFor(baseName);
        var payoutIcon    = payoutRunning ? FontAwesomeIcon.Stop : FontAwesomeIcon.MoneyBillWave;
        var payoutLabel   = payoutRunning ? "Stop Auto Payout" : "Auto Payout";
        UIHelper.CentreNextButtonRow((FontAwesomeIcon.Coins, "Trade Winner"), (payoutIcon, payoutLabel));

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade Winner", $"##HLWinnerTrade_{displayName}"))
                SendTradeRequest.Execute(baseName, this.chatQueue);
        }

        ImGui.SameLine();
        DrawWinnerAutoPayoutButton(displayName, baseName, remaining);
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

    private void DrawWinnerAutoPayoutButton(string displayName, string baseName, long remaining)
    {
        if (this.autoPayoutService.IsRunningFor(baseName))
        {
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Auto Payout", $"##HLStopAutoPayout_{displayName}"))
                this.autoPayoutService.Stop();
            return;
        }

        using var dis   = ImRaii.Disabled(remaining <= 0 || this.autoPayoutService.IsRunning);
        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.MoneyBillWave, "Auto Payout", $"##HLAutoPayout_{displayName}"))
        {
            this.autoPayoutService.Start(
                baseName,
                () => this.higherLowerService.GetWinnerRemaining(displayName),
                () => this.higherLowerService.IsSessionFinished());
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

    private readonly record struct LeaderboardRow(string PlayerName, int BestScore, int TimesPlayed, DateTime FirstBestAt, bool IsEffectiveWinner);
}
