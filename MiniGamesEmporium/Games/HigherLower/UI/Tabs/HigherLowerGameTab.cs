using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ECommons.DalamudServices;

using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Actions;
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
    private const float RightPaneW = 250f;

    private static readonly Vector4 GoldColour          = new(1f, 0.84f, 0f, 1f);
    private static readonly Vector4 HigherColour        = new(0.20f, 0.85f, 0.35f, 1f);
    private static readonly Vector4 LowerColour         = new(0.25f, 0.55f, 1.00f, 1f);
    private static readonly Vector4 HigherDimmed        = new(0.10f, 0.40f, 0.18f, 1f);
    private static readonly Vector4 LowerDimmed         = new(0.10f, 0.25f, 0.50f, 1f);
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

    public HigherLowerGameTab(PluginConfiguration config, HigherLowerService higherLowerService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService, PlayerInfoService playerInfo)
    {
        this.config             = config;
        this.higherLowerService = higherLowerService;
        this.chatQueue          = chatQueue;
        this.autoPayoutService  = autoPayoutService;
        this.playerInfo         = playerInfo;
    }

    public void Draw(bool skipLeadingSpacing = false, float reserveBottom = 0f, Action? drawBottomPanel = null)
    {
        if (!skipLeadingSpacing) ImGui.Spacing();
        var session = this.higherLowerService.GetActiveSession();
        if (session == null || !HigherLowerGameIds.Matches(session.GameName)) return;
        var fullH = MathF.Max(100f, ImGui.GetContentRegionAvail().Y);
        using var split = ImRaii.Table("##HLSplit", 2,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1f, fullH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##HLGameCol",        ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##HLLeaderboardCol", ImGuiTableColumnFlags.WidthFixed, RightPaneW);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        var colTopY = ImGui.GetCursorPosY();
        DrawGamePane(session, MathF.Max(100f, fullH - reserveBottom));
        if (drawBottomPanel != null)
        {
            var targetY = colTopY + fullH - reserveBottom;
            if (targetY > ImGui.GetCursorPosY())
                ImGui.SetCursorPosY(targetY);
            drawBottomPanel();
        }
        ImGui.TableSetColumnIndex(1);
        DrawLeaderboardPane(fullH);
    }

    private void DrawGamePane(ActiveSession session, float height)
    {
        using var pane = ImRaii.Child("##HLGamePane", new Vector2(-1f, height), false);
        if (!pane.Success) return;
        DrawSendRulesButton();
        ImGui.Spacing();
        DrawActiveSessionView(session);
    }

    private void DrawSendRulesButton()
    {
        using var blue = UIHelper.PushBlueButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Scroll, "Send Rules", "##HLSendRules"))
            global::MiniGamesEmporium.Games.HigherLower.Actions.AnnounceRules.Execute(this.config, this.chatQueue);
    }

    private void DrawLeaderboardPane(float height)
    {
        using var pane = ImRaii.Child("##HLLeaderboardPane", new Vector2(-1f, height), false, ImGuiWindowFlags.NoScrollbar);
        if (!pane.Success) return;

        var finished = this.higherLowerService.IsSessionFinished();
        var board    = this.config.HigherLower.SessionLeaderboard;

        var showFinish  = !finished;

        var frameH        = ImGui.GetFrameHeight();
        var sp            = ImGui.GetStyle().ItemSpacing.Y;
        var btnRows       = showFinish ? 1 : 0;
        var ctrlH         = btnRows > 0 ? frameH * btnRows + sp * (btnRows + 1) + 4f : 0f;
        var boardSectionH = MathF.Max(40f, height - ctrlH - sp);

        {
            using var boardSection = ImRaii.Child("##HLBoardSection", new Vector2(-1f, boardSectionH), false);
            if (boardSection.Success)
            {
                CentreText("Leaderboard", EmporiumNeonTheme.HigherLowerOrange, ImGui.GetCursorPosX(), ImGui.GetContentRegionAvail().X);
                ImGui.Separator();
                if (board.Count == 0)
                    ImGui.TextDisabled("No one has played yet.");
                else
                    DrawLeaderboardTable(BuildLeaderboardRows(board));
            }
        }

        if (btnRows > 0)
            DrawRightPaneControls(showFinish, board.Count > 0);
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
            var col = row.IsEffectiveWinner ? new Vector4(1f, 0.84f, 0f, 1f) : EmporiumNeonTheme.NeonCyan;
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
            DrawPartyMemberList();
            return;
        }

        var turn = this.higherLowerService.GetActiveTurn();

        if (turn != null && turn.IsGameOver) { DrawGameOverScreen(session, turn); return; }

        DrawPlayerHeader(session);
        ImGui.Spacing();

        if (!session.PaymentVerified)
        {
            DrawTakeBetPhase(session);
            return;
        }

        DrawFixedActionPanel(session, turn);
    }

    private void DrawPartyMemberList()
    {
        ImGui.Spacing();
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;

        CentreText("Select Player from Party", EmporiumNeonTheme.HigherLowerOrange, startX, avail);
        ImGui.Spacing();

        var members = GetPartyMembers();
        if (members.Count == 0)
        {
            const string hint = "No other party members found. Invite the player to your party first.";
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(hint).X) * 0.5f));
            ImGui.TextDisabled(hint);
            return;
        }

        ImGui.Separator();
        ImGui.Spacing();

        using var table = ImRaii.Table("##HLPartyList", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(-1, 0));
        if (!table.Success) return;
        ImGui.TableSetupColumn("Player",    ImGuiTableColumnFlags.WidthStretch);
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
        var result    = new List<(string, string, string)>();
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

    private void DrawPlayerHeader(ActiveSession session)
    {
        var avail  = ImGui.GetContentRegionAvail().X;
        var startX = ImGui.GetCursorPosX();
        var turn   = this.higherLowerService.GetActiveTurn();
        var sp     = ImGui.GetStyle().ItemSpacing.X;

        var fullName = BuildDisplayName(session);
        var btnW     = UIHelper.CalcButtonSize(FontAwesomeIcon.ExclamationTriangle, "End Turn Early").X;

        ImGui.SetWindowFontScale(1.4f);
        var nameW = ImGui.CalcTextSize(fullName).X;
        ImGui.SetWindowFontScale(1.0f);
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - nameW - sp - btnW) * 0.5f));
        ImGui.SetWindowFontScale(1.4f);
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, fullName);
        ImGui.SetWindowFontScale(1.0f);
        ImGui.SameLine();
        using (UIHelper.PushRedButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.ExclamationTriangle, "End Turn Early", "##HLEndEarlyBtn")
                && ImGui.GetIO().KeyCtrl)
                this.higherLowerService.EndCurrentTurn();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hold CTRL + click to end this player's turn");

        var statusText   = GetStatusText(session, turn, this.config.HigherLower.DiceSides);
        var statusColour = GetStatusColour(session, turn);
        ImGui.SetWindowFontScale(1.15f);
        var statusW = ImGui.CalcTextSize(statusText).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - statusW) * 0.5f));
        ImGui.TextColored(statusColour, statusText);
        ImGui.SetWindowFontScale(1.0f);
    }

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

    private void DrawTakeBetPhase(ActiveSession session)
    {
        ImGui.Separator();
        ImGui.Spacing();
        using var split = ImRaii.Table("##HLTakeBetSplit", 2, ImGuiTableFlags.BordersInnerV, new Vector2(-1f, 0f));
        if (split.Success)
        {
            ImGui.TableSetupColumn("##HLTakeBetLeft",  ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##HLTakeBetRight", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            DrawPrimaryBetActions(session);
            ImGui.TableSetColumnIndex(1);
            DrawBuyerSection(session);
        }
        ImGui.Spacing();
        DrawBeginGameSection(session);
    }

    private void DrawPrimaryBetActions(ActiveSession session)
    {
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;

        CentreText("Take Bet", EmporiumNeonTheme.WarnAmber, startX, avail);
        ImGui.Spacing();

        var reqW = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request Gil").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - reqW) * 0.5f));
        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil", "##HLRequestGil"))
            {
                var tellName = BuildDisplayName(session);
                RequestEntryFee.Execute(tellName, this.config, this.chatQueue);
            }
        }

        ImGui.Spacing();

        var tradeW = UIHelper.CalcButtonSize(FontAwesomeIcon.Coins, "Trade").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - tradeW) * 0.5f));
        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", "##HLTradeBtn"))
                SendTradeRequest.Execute(session.PlayerName, this.chatQueue);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        const string bypassWarn = "No payment required";
        const string bypassSub  = "Gil will not be added to pot";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(bypassWarn).X) * 0.5f));
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, bypassWarn);
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(bypassSub).X) * 0.5f));
        ImGui.TextDisabled(bypassSub);
        ImGui.Spacing();

        var bypassW = UIHelper.CalcButtonSize(FontAwesomeIcon.UserShield, "Skip Payment").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - bypassW) * 0.5f));
        using (UIHelper.PushRedButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.UserShield, "Skip Payment", "##HLBypassPayment"))
                this.higherLowerService.StartGame();
        }
    }

    private void DrawBuyerSection(ActiveSession session)
    {
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;
        var style  = ImGui.GetStyle();

        CentreText("Paying for another player", EmporiumNeonTheme.NeonMagenta, startX, avail);
        ImGui.Spacing();

        var buyer = this.higherLowerService.GetBuyer();
        if (!string.IsNullOrEmpty(buyer))
        {
            var clearW = UIHelper.CalcButtonSize(FontAwesomeIcon.Times, "Clear").X;
            var rowW   = ImGui.CalcTextSize("Buyer:").X + style.ItemSpacing.X + ImGui.CalcTextSize(buyer).X + style.ItemSpacing.X + clearW;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - rowW) * 0.5f));
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

            var reqBuyerW = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - reqBuyerW) * 0.5f));
            using (UIHelper.PushBlueButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", "##HLBuyerRequestGil"))
                    SendTellBuyerRequest.Execute(buyer, session.PlayerName, this.config, this.chatQueue);
            }
            ImGui.Spacing();

            var tradeBuyerW = UIHelper.CalcButtonSize(FontAwesomeIcon.Coins, "Trade (Buyer)").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - tradeBuyerW) * 0.5f));
            using (UIHelper.PushAmberButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade (Buyer)", "##HLBuyerTrade"))
                    SendTradeRequest.Execute(buyer, this.chatQueue);
            }
        }
        else
        {
            var (charName, worldName) = GetCurrentTarget();
            if (!string.IsNullOrEmpty(charName))
            {
                var targetedRowW = ImGui.CalcTextSize("Targeted:").X + style.ItemSpacing.X + ImGui.CalcTextSize(charName).X;
                ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - targetedRowW) * 0.5f));
                ImGui.TextDisabled("Targeted:");
                ImGui.SameLine();
                ImGui.TextUnformatted(charName);
                ImGui.Spacing();

                var setBuyerW = UIHelper.CalcButtonSize(FontAwesomeIcon.UserCheck, "Set as Buyer").X;
                ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - setBuyerW) * 0.5f));
                using (UIHelper.PushGreenButtonColours())
                {
                    if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Set as Buyer", "##HLSetBuyer"))
                    {
                        var full = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
                        this.higherLowerService.SetBuyer(full);
                    }
                }
            }
            else
            {
                const string hint = "Target a player in-game to set them as the buyer.";
                ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(hint).X) * 0.5f));
                ImGui.TextDisabled(hint);
            }
        }
        ImGui.Spacing();
    }

    private void DrawBeginGameSection(ActiveSession session)
    {
        ImGui.Separator();
        ImGui.Spacing();
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;

        CentreText("Begin Game", EmporiumNeonTheme.NeonCyan, startX, avail);
        ImGui.Spacing();

        var desc = session.AmountTraded > 0
            ? $"{session.AmountTraded:N0} Gil received"
            : "No trade recorded yet.";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(desc).X) * 0.5f));
        ImGui.TextDisabled(desc);
        ImGui.Spacing();

        var beginW = UIHelper.CalcButtonSize(FontAwesomeIcon.Play, "Begin Game").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - beginW) * 0.5f));
        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Play, "Begin Game", "##HLBeginGame"))
            this.higherLowerService.StartGame();
    }

    private void DrawFixedActionPanel(ActiveSession session, HigherLowerTurnState? turn)
    {
        ImGui.Separator();
        ImGui.Spacing();
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;
        var dice   = this.config.HigherLower.DiceSides;
        var sp     = ImGui.GetStyle().ItemSpacing.X;

        var hasRoll        = turn?.RollLog.Count > 0;
        var guessConfirmed = turn?.GuessConfirmed == true;
        var detectedGuess  = turn?.DetectedGuess;

        DrawNumberCard(turn, startX, avail);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (!hasRoll || guessConfirmed)
        {
            var rollLabel    = $"Roll {dice}";
            var showLetsPlay = !hasRoll;

            if (showLetsPlay)
            {
                var letsPlayW = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Let's Play Message").X;
                var rollW     = UIHelper.CalcButtonSize(FontAwesomeIcon.Dice, rollLabel).X;
                ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - letsPlayW - rollW - sp * 2f) * 0.5f));
                using (UIHelper.PushAmberButtonColours())
                {
                    if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Let's Play Message", "##HLLetsPlay"))
                    {
                        AnnounceLetsPlay.Execute(BuildDisplayName(session), this.config, this.chatQueue);
                    }
                }
                ImGui.SameLine();
                ImGui.Spacing();
                ImGui.SameLine();
            }
            else
            {
                var rollW = UIHelper.CalcButtonSize(FontAwesomeIcon.Dice, rollLabel).X;
                ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - rollW) * 0.5f));
            }

            using (UIHelper.PushBlueButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Dice, rollLabel, "##HLRollBtn"))
                    this.chatQueue.Enqueue($"/dice party {dice}");
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        if (hasRoll && !guessConfirmed)
        {
            var askW = UIHelper.CalcButtonSize(FontAwesomeIcon.Question, "Ask Guess").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - askW) * 0.5f));
            using (UIHelper.PushBlueButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Question, "Ask Guess", "##HLAskGuess") && turn != null)
                {
                    AnnounceAskGuess.Execute(BuildDisplayName(session), turn.RollLog[^1], this.config, this.chatQueue);
                }
            }

            ImGui.Spacing();

            var higherDetected = detectedGuess == 1;
            var lowerDetected  = detectedGuess == -1;
            var hasDetection   = detectedGuess.HasValue;
            var higherBtnW     = UIHelper.CalcButtonSize(FontAwesomeIcon.ArrowUp,   "Higher").X;
            var lowerBtnW      = UIHelper.CalcButtonSize(FontAwesomeIcon.ArrowDown, "Lower").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - higherBtnW - lowerBtnW - sp * 2f) * 0.5f));

            if (higherDetected) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 1f));
            using (new ButtonColourScope(
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
            using (new ButtonColourScope(
                hasDetection ? (lowerDetected ? DetectedYellow      : DimGrey)      : LowerColour,
                hasDetection ? (lowerDetected ? DetectedYellowHover : DimGreyHover) : new Vector4(0.32f, 0.68f, 1f, 1f),
                hasDetection ? (lowerDetected ? DetectedYellowAct   : DimGreyAct)   : new Vector4(0.22f, 0.48f, 0.80f, 1f)))
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowDown, "Lower", "##HLLowerBtn"))
                    this.higherLowerService.RegisterGuess(isHigher: false);
            }
            if (lowerDetected) ImGui.PopStyleColor();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        if (guessConfirmed)
        {
            var cancelW = UIHelper.CalcButtonSize(FontAwesomeIcon.Undo, "Cancel Guess").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - cancelW) * 0.5f));
            using (UIHelper.PushRedButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Undo, "Cancel Guess", "##HLCancelGuess"))
                    this.higherLowerService.ClearConfirmedGuess();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        DrawInlineGameLog();
    }

    private void DrawNumberCard(HigherLowerTurnState? turn, float startX, float avail)
    {
        var curr = turn?.RollLog.Count > 0 ? turn.RollLog[^1].ToString() : "--";
        ImGui.SetWindowFontScale(2.8f);
        var numW = ImGui.CalcTextSize(curr).X;
        ImGui.SetWindowFontScale(1.0f);
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - numW) * 0.5f));
        ImGui.SetWindowFontScale(2.8f);
        ImGui.TextColored(EmporiumNeonTheme.HigherLowerOrange, curr);
        ImGui.SetWindowFontScale(1.0f);

        using var tbl = ImRaii.Table("##HLNumInfo", 2, ImGuiTableFlags.None, new Vector2(avail, 0));
        if (!tbl.Success) return;
        ImGui.TableSetupColumn("##HLPrev",   ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##HLRounds", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        var prevText = (turn?.RollLog.Count >= 2) ? $"  Previous: {turn!.RollLog[^2]}" : "  Previous: -";
        ImGui.TextDisabled(prevText);
        ImGui.TableSetColumnIndex(1);
        var roundsLbl = $"Rounds Correct: {turn?.RoundsCorrect ?? 0}";
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f,
            ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(roundsLbl).X - ImGui.GetStyle().ItemSpacing.X));
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, roundsLbl);
    }

    private void DrawInlineGameLog()
    {
        ImGui.Separator();
        ImGui.Spacing();
        const float LogH = 200f;
        using var child = ImRaii.Child("##HLInlineLog", new Vector2(-1f, LogH), true);
        if (!child.Success) return;
        var log = this.higherLowerService.GetGameLog();
        if (log.Count == 0) { ImGui.TextDisabled("No events yet."); return; }
        foreach (var entry in log)
            ImGui.TextUnformatted(entry);
        ImGui.SetScrollHereY(1.0f);
    }

    private void DrawRightPaneControls(bool showFinish, bool canFinish)
    {
        ImGui.Separator();
        ImGui.Spacing();
        var startX = ImGui.GetCursorPosX();
        var avail  = ImGui.GetContentRegionAvail().X;

        if (showFinish)
        {
            var finishW = UIHelper.CalcButtonSize(FontAwesomeIcon.Trophy, "Finish Game").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - finishW) * 0.5f));
            using (ImRaii.Disabled(!canFinish))
            using (UIHelper.PushGreenButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Trophy, "Finish Game", "##HLFinishGame"))
                    this.higherLowerService.FinishSession();
            }
            if (!canFinish && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("No players have finished a turn yet.");
        }
    }

    private void DrawGameOverScreen(ActiveSession session, HigherLowerTurnState turn)
    {
        var avail  = ImGui.GetContentRegionAvail().X;
        var startX = ImGui.GetCursorPosX();

        var name = BuildDisplayName(session);
        ImGui.SetWindowFontScale(1.6f);
        var nameW = ImGui.CalcTextSize(name).X;
        ImGui.SetWindowFontScale(1.0f);
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - nameW) * 0.5f));
        ImGui.SetWindowFontScale(1.6f);
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, name);
        ImGui.SetWindowFontScale(1.0f);

        const string subtitle = "TURN COMPLETE";
        var subtitleW = ImGui.CalcTextSize(subtitle).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - subtitleW) * 0.5f));
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, subtitle);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var scoreLabel = $"Rounds Correct: {turn.RoundsCorrect}";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(scoreLabel).X) * 0.5f));
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, scoreLabel);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var isLeading   = this.higherLowerService.IsCurrentlyLeading(turn.RoundsCorrect);
        var target      = this.higherLowerService.GetLeadTarget(turn.RoundsCorrect);
        var announceIcon  = isLeading ? FontAwesomeIcon.Star : FontAwesomeIcon.Medal;
        var announceLabel = isLeading ? "Announce Lead" : "Announce Score";
        var announceW = UIHelper.CalcButtonSize(announceIcon, announceLabel).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - announceW) * 0.5f));
        using (isLeading ? UIHelper.PushGreenButtonColours() : UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(announceIcon, announceLabel, "##HLAnnounceGameOver"))
                AnnounceHigherLowerLoss.ExecuteLeaderboardAnnounce(
                    BuildDisplayName(session), turn.RoundsCorrect, isLeading, target,
                    this.config, this.chatQueue);
        }

        ImGui.Spacing();

        DrawEndTurnButton(startX, avail);
    }

    private void DrawSessionWinnerScreen()
    {
        var avail  = ImGui.GetContentRegionAvail().X;
        var startX = ImGui.GetCursorPosX();

        const string title = "SESSION COMPLETE";
        ImGui.SetWindowFontScale(1.6f);
        var titleW = ImGui.CalcTextSize(title).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - titleW) * 0.5f));
        ImGui.TextColored(GoldColour, title);
        ImGui.SetWindowFontScale(1.0f);

        var winners  = this.higherLowerService.GetSessionWinners();
        var totalPot = this.higherLowerService.GetTotalPot();
        var share    = this.higherLowerService.GetSessionWinnerShare();

        var potLabel = $"Total Pot: {totalPot:N0} Gil";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(potLabel).X) * 0.5f));
        ImGui.TextColored(GoldColour, potLabel);

        if (winners.Count > 1)
        {
            var shareLabel = $"{winners.Count} winners - {share:N0} Gil each";
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(shareLabel).X) * 0.5f));
            ImGui.TextColored(EmporiumNeonTheme.NeonCyan, shareLabel);
        }

        ImGui.Spacing();

        var resumeW = UIHelper.CalcButtonSize(FontAwesomeIcon.Undo, "Resume Session").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - resumeW) * 0.5f));
        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Undo, "Resume Session", "##HLResumeSession"))
                this.higherLowerService.ResumeFinishedSession();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Undo finishing and keep playing - the leaderboard is kept.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (winners.Count == 0)
        {
            const string none = "No winners to pay out.";
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(none).X) * 0.5f));
            ImGui.TextDisabled(none);
            return;
        }

        for (var i = 0; i < winners.Count; i++)
        {
            DrawWinnerPayoutCard(winners[i].Name, winners[i].Rounds, totalPot, share, avail, startX);
            if (i < winners.Count - 1)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
            }
        }
    }

    private void DrawWinnerPayoutCard(string displayName, int rounds, long totalPot, long share, float avail, float startX)
    {
        var baseName  = PlayerInfoService.StripWorld(displayName);
        var paid      = this.higherLowerService.GetWinnerPaid(displayName);
        var remaining = Math.Max(0L, share - paid);

        ImGui.SetWindowFontScale(1.3f);
        var nameW = ImGui.CalcTextSize(displayName).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - nameW) * 0.5f));
        ImGui.TextColored(GoldColour, displayName);
        ImGui.SetWindowFontScale(1.0f);

        var roundsLabel = $"{rounds} Rounds Correct!";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(roundsLabel).X) * 0.5f));
        ImGui.TextColored(GoldColour, roundsLabel);

        ImGui.Spacing();

        var labelColW = MathF.Max(ImGui.CalcTextSize("Share:").X, MathF.Max(ImGui.CalcTextSize("Traded:").X, ImGui.CalcTextSize("Remaining:").X));
        var valueColW = MathF.Max(ImGui.CalcTextSize($"{share:N0} Gil").X, MathF.Max(ImGui.CalcTextSize($"{paid:N0} Gil").X, ImGui.CalcTextSize($"{remaining:N0} Gil").X));
        var sp        = ImGui.GetStyle().ItemSpacing.X;
        var blockW    = labelColW + sp + valueColW;
        var rowX      = startX + MathF.Max(0f, (avail - blockW) * 0.5f);
        var valueX    = rowX + labelColW + sp;

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(GoldColour, "Share:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(GoldColour, $"{share:N0} Gil");

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, "Traded:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, $"{paid:N0} Gil");

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Remaining:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"{remaining:N0} Gil");

        ImGui.Spacing();

        var announceW = UIHelper.CalcButtonSize(FontAwesomeIcon.Bullhorn, "Announce Winner").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - announceW) * 0.5f));
        using (new ButtonColourScope(
            new Vector4(0.72f, 0.55f, 0f, 1f),
            new Vector4(0.88f, 0.68f, 0f, 1f),
            new Vector4(0.58f, 0.44f, 0f, 1f)))
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Winner", $"##HLAnnounceWinner_{displayName}"))
                AnnounceHigherLowerWin.Execute(displayName, rounds, totalPot, share, this.config, this.chatQueue);
        }

        ImGui.Spacing();

        var tradeBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Coins, "Trade Winner").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - tradeBtnW) * 0.5f));
        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade Winner", $"##HLWinnerTrade_{displayName}"))
                SendTradeRequest.Execute(baseName, this.chatQueue);
        }

        ImGui.Spacing();
        DrawWinnerAutoPayoutButton(displayName, baseName, remaining, avail, startX);
        ImGui.Spacing();
        DrawPayoutProgressBar(share, paid, avail, startX);
    }

    private void DrawWinnerAutoPayoutButton(string displayName, string baseName, long remaining, float avail, float startX)
    {
        if (this.autoPayoutService.IsRunningFor(baseName))
        {
            var stopW = UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, "Stop Auto Payout").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - stopW) * 0.5f));
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Auto Payout", $"##HLStopAutoPayout_{displayName}"))
                this.autoPayoutService.Stop();
        }
        else
        {
            using var dis = ImRaii.Disabled(remaining <= 0 || this.autoPayoutService.IsRunning);
            var autoW = UIHelper.CalcButtonSize(FontAwesomeIcon.MoneyBillWave, "Auto Payout").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - autoW) * 0.5f));
            using var green = UIHelper.PushGreenButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.MoneyBillWave, "Auto Payout", $"##HLAutoPayout_{displayName}"))
            {
                this.autoPayoutService.Start(
                    baseName,
                    () => this.higherLowerService.GetWinnerRemaining(displayName),
                    () => this.higherLowerService.IsSessionFinished());
            }
        }
    }

    private static void DrawPayoutProgressBar(long share, long paid, float avail, float startX)
    {
        var progress   = share > 0 ? MathF.Min(1f, (float)paid / share) : 1f;
        var pctOverlay = $"{progress * 100f:F0}% paid out";
        ImGui.SetCursorPosX(startX);
        ImGui.ProgressBar(progress, new Vector2(avail, ImGui.GetFrameHeight()), pctOverlay);
    }

    private void DrawEndTurnButton(float startX, float avail)
    {
        var endW = UIHelper.CalcButtonSize(FontAwesomeIcon.FlagCheckered, "End Turn").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - endW) * 0.5f));
        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.FlagCheckered, "End Turn", "##HLEndTurnBtn"))
            this.higherLowerService.EndCurrentTurn();
    }

    private static void DrawPotLine(long pot, float startX, float avail)
    {
        var potLabel = $"Pot: {pot:N0} Gil";
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(potLabel).X) * 0.5f));
        ImGui.TextColored(EmporiumNeonTheme.WinGold, potLabel);
    }

    private static void CentreText(string text, Vector4 colour, float startX, float avail)
    {
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - ImGui.CalcTextSize(text).X) * 0.5f));
        ImGui.TextColored(colour, text);
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

    private readonly struct ButtonColourScope : IDisposable
    {
        public ButtonColourScope(Vector4 normal, Vector4 hovered, Vector4 active)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        normal);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  active);
        }
        public void Dispose() => ImGui.PopStyleColor(3);
    }
}
