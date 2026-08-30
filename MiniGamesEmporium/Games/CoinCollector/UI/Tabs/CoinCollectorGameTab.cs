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
using MiniGamesEmporium.Games.CoinCollector.Actions;
using MiniGamesEmporium.Games.CoinCollector.Models;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the active game view for Coin Collector.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.UI.Tabs;
public sealed class CoinCollectorGameTab
{
    private const float RightPaneW   = 250f;
    private const float MinLogHeight = 120f;
    private const float LogIndent    = 6f;
    private const float TrophySide   = 140f;

    private static readonly Vector4 CardAccent = EmporiumNeonTheme.CoinCollectorIndigo;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);
    private static readonly Vector4 GoldColour = new(1f, 0.84f, 0f, 1f);

    private readonly PluginConfiguration config;
    private readonly CoinCollectorService coinCollectorService;
    private readonly ChatQueueService chatQueue;
    private readonly AutoPayoutService autoPayoutService;
    private readonly PlayerInfoService playerInfo;
    private readonly ISharedImmediateTexture? trophyTexture;
    private readonly ThemedCard card = new();

    public CoinCollectorGameTab(PluginConfiguration config, CoinCollectorService coinCollectorService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService, PlayerInfoService playerInfo)
    {
        this.config               = config;
        this.coinCollectorService = coinCollectorService;
        this.chatQueue            = chatQueue;
        this.autoPayoutService    = autoPayoutService;
        this.playerInfo           = playerInfo;
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
        using var split = ImRaii.Table("##CCSplit", 2,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1f, fullH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##CCGameCol",        ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##CCLeaderboardCol", ImGuiTableColumnFlags.WidthFixed, RightPaneW);
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
        using (UIHelper.PushBlueButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Scroll, "Send Rules", "##CCSendRules"))
                AnnounceRules.Execute(this.config, this.chatQueue);
        ImGui.SameLine();
        using (UIHelper.PushOrangeButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Advertise", "##CCAdvertise"))
                Advertise.Execute(this.config, this.chatQueue);
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
                    DrawLeaderboardTable(BuildLeaderboardRows(board));
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

    private LeaderboardRow[] BuildLeaderboardRows(List<CoinCollectorLeaderboardEntry> board)
    {
        var groups = new Dictionary<string, (int Best, int Plays, DateTime FirstBestAt, bool HasWin)>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in board)
        {
            if (!groups.TryGetValue(e.PlayerName, out var g))
            {
                groups[e.PlayerName] = (e.Coins, 1, e.PlayedAt, e.IsWinner);
                continue;
            }
            var firstBestAt = e.Coins > g.Best ? e.PlayedAt
                            : e.Coins == g.Best && e.PlayedAt < g.FirstBestAt ? e.PlayedAt
                            : g.FirstBestAt;
            groups[e.PlayerName] = (Math.Max(g.Best, e.Coins), g.Plays + 1, firstBestAt, g.HasWin || e.IsWinner);
        }
        var allowMultiple = this.config.CoinCollector.AllowMultipleWinners;
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
        if (this.coinCollectorService.IsSessionFinished())
        {
            DrawSessionWinnerScreen();
            return;
        }

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
        var members = GetPartyMembers();
        if (members.Count == 0)
        {
            UIHelper.CentreTextDisabled("No other party members found. Invite the player to your party first.");
            return;
        }

        using var table = ImRaii.Table("##CCPartyList", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(-1f, 0f));
        if (!table.Success) return;
        ImGui.TableSetupColumn("Player",     ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##CCSelBtn", ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableHeadersRow();

        foreach (var (charName, worldName, displayName) in members)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(displayName);
            ImGui.TableSetColumnIndex(1);
            using var green = UIHelper.PushGreenButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Select", $"##CCSel_{charName}"))
                this.coinCollectorService.SetPlayer(charName, worldName);
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
        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Request Gil"),
            (FontAwesomeIcon.Coins, "Trade"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil", "##CCRequestGil"))
                RequestEntryFee.Execute(BuildDisplayName(session), this.config, this.chatQueue);
        }

        ImGui.SameLine();

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", "##CCTradeBtn"))
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

        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Request Gil (Buyer)"),
            (FontAwesomeIcon.Coins, "Trade (Buyer)"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", "##CCBuyerRequestGil"))
                RequestEntryFeeBuyer.Execute(buyer, session.PlayerName, this.config, this.chatQueue);
        }
        ImGui.SameLine();

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade (Buyer)", "##CCBuyerTrade"))
                SendTradeRequest.Execute(buyer, this.chatQueue);
        }
    }

    private void DrawBeginGameBody(ActiveSession session)
    {
        UIHelper.CentreTextDisabled(session.AmountTraded > 0
            ? $"{session.AmountTraded:N0} Gil received"
            : "No trade recorded yet.");
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
        UIHelper.CentreTextScaled(BuildDisplayName(session), EmporiumNeonTheme.WarnAmber, 1.6f);
        ImGui.Spacing();
        UIHelper.CentreText($"Coins Collected: {turn.CoinsCollected}", EmporiumNeonTheme.NeonCyan);

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

        using (UIHelper.PushGreenButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.FlagCheckered, "End Turn", "##CCEndTurnBtn"))
                this.coinCollectorService.EndCurrentTurn();
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
        UIHelper.CentreNextButtonRow((FontAwesomeIcon.Coins, "Trade Winner"), (payoutIcon, payoutLabel));

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade Winner", $"##CCWinnerTrade_{displayName}"))
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
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Auto Payout", $"##CCStopAutoPayout_{displayName}"))
                this.autoPayoutService.Stop();
            return;
        }

        using var dis   = ImRaii.Disabled(remaining <= 0 || this.autoPayoutService.IsRunning);
        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.MoneyBillWave, "Auto Payout", $"##CCAutoPayout_{displayName}"))
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

    private readonly record struct LeaderboardRow(string PlayerName, int BestScore, int TimesPlayed, DateTime FirstBestAt, bool IsEffectiveWinner);
}
