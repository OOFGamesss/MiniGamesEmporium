using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Chat settings tab for Voting Madness message templates.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.UI.Tabs;
public sealed class VotingMadnessChatSettingsTab
{
    private readonly PluginConfiguration config;

    public VotingMadnessChatSettingsTab(PluginConfiguration config) => this.config = config;

    private static readonly (string Token, string Desc)[] Placeholders =
    [
        ("{options}",   "Comma-separated list of voting keywords"),
        ("{standings}", "Current tallies with percentages"),
        ("{winner}",    "Winning option, or tied options listed together"),
        ("{votes}",     "Votes for the winning option (or each tied option)"),
        ("{percent}",   "Winning option share of total votes"),
        ("{totalvotes}", "Total votes cast"),
        ("{voters}",    "Unique players who voted"),
        ("{closetime}", "Configured closing time (ST)"),
        ("{timeleft}",  "Time remaining until closing"),
    ];

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.VotingMadnessLime, "Voting Madness");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        DrawPlaceholderReference();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawManualSection();
    }

    private static void DrawPlaceholderReference()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Available Placeholders");
        ImGui.Spacing();
        var descColX = Placeholders.Max(p => ImGui.CalcTextSize(p.Token).X) + 20f;
        foreach (var (token, desc) in Placeholders)
        {
            ImGui.TextColored(new Vector4(1f, 0.80f, 0.30f, 1f), token);
            ImGui.SameLine(descColX);
            ImGui.TextDisabled(desc);
        }
    }

    private void DrawManualSection()
    {
        var chat = this.config.VotingMadness.Chat;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Manual Trigger Messages");
        ImGui.Spacing();
        DrawMessageField("Announce Options", "Button: 'Announce Options' on the Game panel.",
            "##VMOptionsMsg", () => chat.AnnounceOptionsMessage, v => { chat.AnnounceOptionsMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Vote Started", "Button: 'Vote Started' on the Game panel.",
            "##VMStartedMsg", () => chat.VoteStartedMessage, v => { chat.VoteStartedMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Closing Time", "Button: 'Closing Time' on the Game panel when a close time is set.",
            "##VMClosingMsg", () => chat.AnnounceClosingTimeMessage, v => { chat.AnnounceClosingTimeMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Standings", "Button: 'Standings' on the Game panel.",
            "##VMStandingsMsg", () => chat.AnnounceStandingsMessage, v => { chat.AnnounceStandingsMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Vote Ended", "Sent when Stop Vote is pressed, and available as a button afterwards.",
            "##VMEndedMsg", () => chat.VoteEndedMessage, v => { chat.VoteEndedMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Announce Winning Vote", "Button: 'Announce Winning Vote' after the vote is stopped.",
            "##VMWinnerMsg", () => chat.AnnounceWinningVoteMessage, v => { chat.AnnounceWinningVoteMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Announce Tie", "Used instead of the winning vote message when options are tied.",
            "##VMTieMsg", () => chat.AnnounceTieMessage, v => { chat.AnnounceTieMessage = v; this.config.Save(); });
    }

    private static void DrawMessageField(string title, string hint, string id, Func<string> getter, Action<string> setter)
    {
        ImGui.TextUnformatted(title);
        ImGui.TextDisabled(hint);
        var value = getter();
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText(id, ref value, 500))
            setter(value);
    }
}
