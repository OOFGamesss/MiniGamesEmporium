using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Actions;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Games.VotingMadness.State;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.Utility;

/// <summary>Draws the live Voting Madness bar chart, voter table, shout buttons and stats.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.UI.Tabs;
public sealed class VotingMadnessGameTab
{
    private static readonly Vector4 LimeBtn        = new(0.35f, 0.55f, 0.05f, 1f);
    private static readonly Vector4 LimeBtnHovered = new(0.50f, 0.75f, 0.10f, 1f);
    private static readonly Vector4 LimeBtnActive  = new(0.60f, 0.90f, 0.15f, 1f);

    private static readonly Vector4 CardAccent = EmporiumNeonTheme.VotingMadnessLime;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly PluginConfiguration config;
    private readonly VotingMadnessService service;
    private readonly ChatQueueService chatQueue;
    private readonly ThemedCard card = new();

    public VotingMadnessGameTab(PluginConfiguration config, VotingMadnessService service, ChatQueueService chatQueue)
    {
        this.config    = config;
        this.service   = service;
        this.chatQueue = chatQueue;
    }

    public static float GetStatsHeight(bool hasCloseTime)
    {
        var rowH = ImGui.GetTextLineHeight() + ImGui.GetStyle().CellPadding.Y * 2f;
        var rows = 3 + (hasCloseTime ? 1 : 0);
        return rows * rowH + ImGui.GetStyle().WindowPadding.Y * 2f + 4f;
    }

    public void Draw()
    {
        var state = this.service.GetState();
        if (state == null) return;

        using var pane = ImRaii.Child("##VMGamePane", Vector2.Zero, false,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!pane.Success) return;

        var statsH = GetStatsHeight(state.HasCloseTime);
        var bodyH  = MathF.Max(80f, ImGui.GetContentRegionAvail().Y - statsH - ImGui.GetStyle().ItemSpacing.Y);
        using (var body = ImRaii.Child("##VMGameBody", new Vector2(-1f, bodyH), false))
        {
            if (body.Success)
            {
                this.card.Draw("##VMShoutsCard", "Shouts", CardAccent, CardTitle, () => DrawShouts(state));
                this.card.Draw("##VMActionsCard", "Actions", CardAccent, CardTitle, () => DrawActions(state));
                this.card.Draw("##VMResultsCard", "Results", CardAccent, CardTitle, () => DrawResultsBody(state));

                var rows    = this.service.GetPlayerRows();
                var votersH = MathF.Max(160f, ImGui.GetContentRegionAvail().Y - ThemedCard.ChromeHeight());
                this.card.Draw("##VMVotersCard", $"Voters ({rows.Count})", CardAccent, CardTitle, votersH,
                    () => DrawVoterTable(rows));
            }
        }

        var targetY = ImGui.GetContentRegionMax().Y - statsH;
        if (targetY > ImGui.GetCursorPosY())
            ImGui.SetCursorPosY(targetY);
        DrawBottomStats(state);
    }

    private void DrawResultsBody(VotingMadnessState state)
    {
        DrawStatus(state);
        ImGui.Spacing();
        DrawBarChart(state);
    }

    private void DrawStatus(VotingMadnessState state)
    {
        if (!state.IsVotingClosed)
        {
            ImGui.TextColored(EmporiumNeonTheme.VotingMadnessLime, "Listening for votes");
            return;
        }

        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Voting closed");
        var (winners, votes, percent, isTie) = this.service.GetResult();
        if (winners.Count == 0)
            ImGui.TextDisabled("No votes were cast.");
        else if (isTie)
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber,
                $"Tie: {string.Join(", ", winners)} ({percent:0}% / {votes} votes each)");
        else
            ImGui.TextColored(EmporiumNeonTheme.VotingMadnessLime,
                $"Leading: {winners[0]} ({percent:0}% / {votes} votes)");
    }

    private void DrawBarChart(VotingMadnessState state)
    {
        var total  = Math.Max(1, this.service.ComputeTotalVotes());
        var barH   = 22f;
        var avail  = ImGui.GetContentRegionAvail().X;
        var startX = ImGui.GetCursorPosX();
        var labelW = 0f;
        foreach (var option in state.Options)
            labelW = MathF.Max(labelW, ImGui.CalcTextSize(option.Keyword).X);
        labelW += 8f;
        var countW = 48f;
        var barW   = MathF.Max(40f, avail - labelW - countW - 16f);

        foreach (var option in state.Options)
        {
            var count = this.service.CountVotesFor(option.Keyword);
            var frac  = count / (float)total;
            var colour = new Vector4(option.ColourR, option.ColourG, option.ColourB, option.ColourA);

            ImGui.TextColored(colour, option.Keyword);
            ImGui.SameLine(startX + labelW);
            var cursor = ImGui.GetCursorScreenPos();
            var dl = ImGui.GetWindowDrawList();
            dl.AddRectFilled(cursor, cursor + new Vector2(barW, barH), ImGui.GetColorU32(new Vector4(0.12f, 0.12f, 0.14f, 1f)), 3f);
            if (frac > 0f)
                dl.AddRectFilled(cursor, cursor + new Vector2(barW * frac, barH), ImGui.GetColorU32(colour), 3f);
            ImGui.Dummy(new Vector2(barW, barH));
            ImGui.SameLine();
            ImGui.TextUnformatted(count.ToString());
        }
    }

    public void DrawSessionActionButtons()
    {
        using (UIHelper.PushBlueButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Scroll, "Send Rules", "##VMSendRules"))
                AnnounceRules.Execute(this.config, this.service, this.chatQueue);
        ImGui.SameLine();
        using (UIHelper.PushOrangeButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Advertise", "##VMAdvertise"))
                Advertise.Execute(this.config, this.service, this.chatQueue);
    }

    private void DrawShouts(VotingMadnessState state)
    {
        using (UIHelper.PushButtonColours(LimeBtn, LimeBtnHovered, LimeBtnActive))
            if (UIHelper.IconTextButton(FontAwesomeIcon.List, "Announce Options", "##VMAnnOptions"))
                AnnounceOptions.Execute(this.config, this.service, this.chatQueue);
        ImGui.SameLine();
        using (UIHelper.PushBlueButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Play, "Vote Started", "##VMAnnStarted"))
                AnnounceVoteStarted.Execute(this.config, this.service, this.chatQueue);
        ImGui.SameLine();
        using (UIHelper.PushBlueButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.PollH, "Standings", "##VMAnnStandings"))
                AnnounceStandings.Execute(this.config, this.service, this.chatQueue);

        if (state.HasCloseTime)
        {
            ImGui.SameLine();
            using (UIHelper.PushBlueButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.Clock, "Closing Time", "##VMAnnClosing"))
                    AnnounceClosingTime.Execute(this.config, this.service, this.chatQueue);
        }

        if (state.IsVotingClosed)
        {
            ImGui.SameLine();
            using (UIHelper.PushBlueButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Vote Ended", "##VMAnnEnded"))
                    AnnounceVoteEnded.Execute(this.config, this.service, this.chatQueue);
        }
    }

    private void DrawActions(VotingMadnessState state)
    {
        if (!state.IsVotingClosed)
        {
            using (UIHelper.PushRedButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Vote", "##VMStopVote"))
                {
                    this.service.StopVote();
                    AnnounceVoteEnded.Execute(this.config, this.service, this.chatQueue);
                }
        }
        else
        {
            using (UIHelper.PushGreenButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.Trophy, "Announce Winning Vote", "##VMAnnWinner"))
                    AnnounceWinningVote.Execute(this.config, this.service, this.chatQueue);
        }
    }

    private void DrawVoterTable(IReadOnlyList<(string PlayerName, string World, string VotesLabel)> rows)
    {
        using var table = ImRaii.Table("##VMVoterTable", 4,
            ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(-1f, -1f));
        if (!table.Success) return;
        ImGui.TableSetupColumn("Player", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Vote(s)", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##acts", ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableHeadersRow();

        foreach (var (player, world, votes) in rows)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(player);
            ImGui.TableSetColumnIndex(1);
            ImGui.TextDisabled(string.IsNullOrEmpty(world) ? "-" : world);
            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(votes);
            ImGui.TableSetColumnIndex(3);
            using (UIHelper.PushRedButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Delete", $"##VMDelVote_{player}_{world}"))
                    this.service.ClearPlayerVotes(string.IsNullOrEmpty(world) ? player : $"{player}@{world}");
            }
        }
    }

    private void DrawBottomStats(VotingMadnessState state)
    {
        using var child = ImRaii.Child("##VMStatsPanel", new Vector2(-1f, GetStatsHeight(state.HasCloseTime)), true);
        if (!child.Success) return;
        using var table = ImRaii.Table("##VMStatsTable", 3, ImGuiTableFlags.None, new Vector2(-1f, 0f));
        if (!table.Success) return;
        ImGui.TableSetupColumn("##VMStatsLabel",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##VMStatsAction", ImGuiTableColumnFlags.WidthFixed, 130f);
        ImGui.TableSetupColumn("##VMStatsValue",  ImGuiTableColumnFlags.WidthFixed, 180f);

        DrawStatRow("Total Votes", this.service.ComputeTotalVotes().ToString(), EmporiumNeonTheme.NeonCyan);
        DrawStatRow("Unique Voters", this.service.ComputeUniqueVoters().ToString(), EmporiumNeonTheme.NeonMagenta);

        var leaders = this.service.GetLeadingOptions();
        var leading = leaders.Count == 0
            ? "None"
            : leaders.Count == 1 ? leaders[0] : $"Tie: {string.Join(", ", leaders)}";
        DrawStatRow("Leading Option", leading, EmporiumNeonTheme.VotingMadnessLime);

        if (state.HasCloseTime)
        {
            var timeLeft = ServerTimeUtil.FormatTimeLeft(state.CloseAtUtc);
            var colour   = timeLeft == "Closed" ? EmporiumNeonTheme.WarnAmber : EmporiumNeonTheme.VotingMadnessLime;
            DrawStatRow("Time Left", timeLeft, colour);
        }
    }

    private static void DrawStatRow(string label, string value, Vector4 valueColour)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(valueColour, value);
    }

}
