using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Actions;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;

/// <summary>Renders the inline session leaderboard for Higher/Lower.</summary>

namespace MiniGamesEmporium.Games.HigherLower.UI.Tabs;
public sealed class HigherLowerLeaderboardTab
{
    private const int MaxWinnerNamesShown = 2;

    private static readonly Vector4 YellColour        = new(0.72f, 0.55f, 0f, 1f);
    private static readonly Vector4 YellColourHovered = new(0.88f, 0.68f, 0f, 1f);
    private static readonly Vector4 YellColourActive  = new(0.58f, 0.44f, 0f, 1f);
    private static readonly Vector4 WinnerRowColour   = new(1f, 0.84f, 0f, 1f);

    private readonly PluginConfiguration config;
    private readonly HigherLowerService higherLowerService;
    private readonly ChatQueueService chatQueue;
    private readonly HistoryService historyService;
    private int donationInput = 0;

    public HigherLowerLeaderboardTab(PluginConfiguration config, HigherLowerService higherLowerService, ChatQueueService chatQueue, HistoryService historyService)
    {
        this.config             = config;
        this.higherLowerService = higherLowerService;
        this.chatQueue          = chatQueue;
        this.historyService     = historyService;
    }

    private static float ChildHeight(bool showKept)
    {
        var rowH   = ImGui.GetTextLineHeight() + ImGui.GetStyle().CellPadding.Y * 2f;
        var inputH = ImGui.GetFrameHeight()    + ImGui.GetStyle().CellPadding.Y * 2f;
        var extraWinnerLines = MaxWinnerNamesShown * (ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y);
        var rows = 6 + (showKept ? 1 : 0);
        return rows * rowH + inputH + extraWinnerLines + ImGui.GetStyle().WindowPadding.Y * 2f + 4f;
    }

    public static float GetInlineHeight() => GetInlineHeight(showKept: false);
    public static float GetInlineHeight(bool showKept) => ChildHeight(showKept) + ImGui.GetStyle().ItemSpacing.Y * 2f;

    public void DrawInline()
    {
        var hl       = this.config.HigherLower;
        var totalPot = this.higherLowerService.GetTotalPot();
        var keptFromTrades = HigherLowerService.ComputeTradesHeldBack(this.config);
        var showKept = this.config.HigherLower.TradesToPotPercent < 100;
        var board    = hl.SessionLeaderboard;
        var topScore = board.Count > 0 ? board.Max(e => e.RoundsCorrect) : 0;
        var topRounds = board.Count > 0 ? topScore.ToString() : "--";
        var leaders = this.higherLowerService.GetSessionWinners()
            .Select(w => w.Name)
            .ToList();
        var winnerLines = BuildWinnerLines(leaders);

        ImGui.Spacing();
        using var child = ImRaii.Child("##HLStatsPanel", new Vector2(-1, ChildHeight(showKept)), true);
        if (!child.Success) return;

        var targetX = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;

        using var table = ImRaii.Table("##HLStatsTable", 3, ImGuiTableFlags.None, new Vector2(-1, 0));
        if (!table.Success) return;
        ImGui.TableSetupColumn("##HLStatsLabel",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##HLStatsAction", ImGuiTableColumnFlags.WidthFixed, 130f);
        ImGui.TableSetupColumn("##HLStatsValue",  ImGuiTableColumnFlags.WidthFixed, 180f);

        DrawTotalPotRow(totalPot);
        DrawRow("Boosted Pot",       $"{hl.BoostedPot:N0} Gil",         EmporiumNeonTheme.WinGold);
        DrawRow("Taken in Trades",   $"{hl.SessionTradedTotal:N0} Gil", EmporiumNeonTheme.NeonCyan);
        if (showKept)
            DrawRow("Kept from Trades", $"{keptFromTrades:N0} Gil",     EmporiumNeonTheme.WarnAmber);
        DrawRow("Players Played",    hl.PlayersPlayed.ToString(),        EmporiumNeonTheme.NeonMagenta);
        DrawRow("Highest Rounds",    topRounds,                          EmporiumNeonTheme.NeonCyan);
        DrawMultiLineRow("Currently Winning", winnerLines,               EmporiumNeonTheme.WinGold, targetX, rightAlign: leaders.Count > 0);
        DrawDonationRow();
    }

    private void DrawDonationRow()
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled("Adjust Pot (Gil)");
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputInt("##HLDonation", ref this.donationInput, 0, 0);
        ImGui.TableSetColumnIndex(2);
        using (UIHelper.PushGreenButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Add", "##HLAddDonation") && this.donationInput > 0)
            {
                AddDonation.Execute(this.config, this.historyService, this.donationInput);
                this.donationInput = 0;
            }
        ImGui.SameLine();
        using (UIHelper.PushRedButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Minus, "Remove", "##HLRemoveDonation") && this.donationInput > 0)
            {
                RemoveDonation.Execute(this.config, this.historyService, this.donationInput);
                this.donationInput = 0;
            }
    }

    public void DrawFullLeaderboard()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.HigherLowerOrange, "Session Leaderboard");
        ImGui.Separator();
        ImGui.Spacing();

        var board = this.config.HigherLower.SessionLeaderboard;
        if (board.Count == 0)
        {
            ImGui.TextDisabled("No players have finished a turn yet.");
            return;
        }

        var winnerCount = this.higherLowerService.GetWinnerCount();
        var share       = this.higherLowerService.GetPerWinnerShare();

        if (winnerCount > 0)
        {
            var pot = this.higherLowerService.GetTotalPot();
            ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{winnerCount} winner(s) - {share:N0} Gil each (pot: {pot:N0} Gil)");
            ImGui.Spacing();
        }

        using var tbl = ImRaii.Table("##HLLeaderboard", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(-1, 0));
        if (!tbl.Success) return;
        ImGui.TableSetupColumn("Rank",   ImGuiTableColumnFlags.WidthFixed,   50f);
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Rounds", ImGuiTableColumnFlags.WidthFixed,   70f);
        ImGui.TableHeadersRow();

        var sorted = board.OrderByDescending(e => e.RoundsCorrect).ThenBy(e => e.PlayedAt).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            var entry = sorted[i];
            ImGui.TableNextRow();
            var col = entry.IsWinner ? WinnerRowColour : EmporiumNeonTheme.NeonCyan;
            ImGui.TableSetColumnIndex(0);
            ImGui.TextColored(col, (i + 1).ToString());
            ImGui.TableSetColumnIndex(1);
            var displayName = entry.IsWinner ? $"{entry.PlayerName} WINNER" : entry.PlayerName;
            ImGui.TextColored(col, displayName);
            ImGui.TableSetColumnIndex(2);
            ImGui.TextColored(col, entry.RoundsCorrect.ToString());
        }
    }

    private void DrawTotalPotRow(long totalPot)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled("Total Pot");
        ImGui.TableSetColumnIndex(1);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4f, 0f));
        ImGui.PushStyleColor(ImGuiCol.Button,        YellColour);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellColourHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellColourActive);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Pot", "##HLYellPot");
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar();
        if (clicked)
        {
            AnnouncePot.Execute(totalPot, this.config, this.chatQueue);
        }
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{totalPot:N0} Gil");
    }

    private static void DrawRow(string label, string value, Vector4 valueColour)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(valueColour, value);
    }

    private static void DrawMultiLineRow(string label, IReadOnlyList<string> lines, Vector4 valueColour, float targetX, bool rightAlign)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(2);
        foreach (var line in lines)
        {
            if (rightAlign)
                PositionValueAt(targetX, line);
            ImGui.TextColored(valueColour, line);
        }
    }

    private static void PositionValueAt(float targetX, string text) =>
        ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), targetX - ImGui.CalcTextSize(text).X));

    private static List<string> BuildWinnerLines(List<string> leaders)
    {
        if (leaders.Count == 0) return ["--"];
        if (leaders.Count <= MaxWinnerNamesShown + 1) return leaders;
        var lines = leaders.Take(MaxWinnerNamesShown).ToList();
        lines.Add($"+{leaders.Count - MaxWinnerNamesShown} more");
        return lines;
    }
}
