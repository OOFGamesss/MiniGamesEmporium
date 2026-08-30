using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Chat settings tab for Voting Madness message templates.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.UI.Tabs;
public sealed class VotingMadnessChatSettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.VotingMadnessLime;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

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
        ImGui.TextColored(CardAccent, "Voting Madness");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        this.card.Draw("##VMChatPlaceholders", "Available Placeholders", CardAccent, CardTitle, DrawPlaceholderReference);
        this.card.Draw("##VMChatManual", "Manual Trigger Messages", CardAccent, CardTitle, DrawManualSection);
    }

    private static void DrawPlaceholderReference() => PlaceholderReference.Draw(Placeholders);

    private void DrawManualSection()
    {
        var chat = this.config.VotingMadness.Chat;
        DrawMessageField("Advertise", "Button: 'Advertise' on the top-left of the session control bar. Shouted to the zone to pull in more voters.",
            "##VMAdvertiseMsg", () => chat.AdvertiseMessage, v => { chat.AdvertiseMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMultilineMessageField("Rules", "Button: 'Send Rules' on the top-left of the session control bar. Each line is sent as its own message.",
            "##VMRulesMsg", () => chat.RulesMessage, v => { chat.RulesMessage = v; this.config.Save(); });
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

    private static void DrawMultilineMessageField(string title, string hint, string id, Func<string> getter, Action<string> setter)
    {
        ImGui.TextUnformatted(title);
        ImGui.TextDisabled(hint);
        var value = getter();
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputTextMultiline(id, ref value, 1024, new Vector2(-1f, ImGui.GetTextLineHeight() * 6f)))
            setter(value);
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
