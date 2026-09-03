using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Chat settings tab for Coin Collector message templates.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.UI.Tabs;
public sealed class CoinCollectorChatSettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.CoinCollectorIndigo;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private const float NameCapFieldWidth = 180f;

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;

    public CoinCollectorChatSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Coin Collector");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        this.card.Draw("##CCChatPlaceholders", "Available Placeholders", CardAccent, CardTitle, DrawPlaceholderReference);
        this.card.Draw("##CCChatManual", "Manual Trigger Messages", CardAccent, CardTitle, DrawManualSection);
        this.card.Draw("##CCChatAuto", "Auto-Send Toggles", CardAccent, CardTitle, DrawAutoSection);
    }

    private static readonly (string Token, string Desc)[] Placeholders =
    [
        ("{player}",        "Player name; @World included only for /tell messages"),
        ("{buyername}",     "Name of the player paying on someone else's behalf (Request Gil (Buyer) message only)"),
        ("{cost}",          "Entry cost in Gil"),
        ("{coins}",         "Number of coins collected so far (Ask to Roll (With Coins) message only)"),
        ("{totalpot}",      "Total pot in Gil"),
        ("{winningamount}", "Winner's share of the pot (Win Shout message only)"),
        ("{rollmax}",       "Number to roll next, blank for the opening roll (Ask to Roll message only)"),
        ("{highestcoins}",  "Coins needed to lead (or the player's own coins when winning) - Announce messages only"),
        ("{position}",      "The player's current leaderboard position, or 'next' if they have not played yet"),
        ("{leader}",        "Name of whoever is currently in the lead, comma separated for a tie"),
        ("{leaderboard}",   "Ranked list of the top players and their coins, e.g. 1. Alice (7), 2. Bob (5)"),
        ("{wrongmax}",      "The number the player wrongly rolled out of (Wrong Roll message only)"),
        ("{attempt}",       "Which paid turn the player is on, when they bought several"),
        ("{attempts}",      "How many paid turns the player bought in total"),
    ];

    private static void DrawPlaceholderReference() => PlaceholderReference.Draw(Placeholders);

    private void DrawManualSection()
    {
        var chat = this.config.CoinCollector.Chat;
        DrawMessageField(
            "Advertise",
            "Button: 'Advertise' on the top-left of the session control bar. Shouted to the zone to pull in new players.",
            "##CCAdvertiseMsg",
            () => chat.AdvertiseMessage,
            v  => { chat.AdvertiseMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMultilineMessageField(
            "Rules",
            "Button: 'Send Rules' on the top-left of the session control bar. Each line is sent as its own message.",
            "##CCRulesMsg",
            () => chat.RulesMessage,
            v  => { chat.RulesMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Pot",
            "Button: 'Announce Pot' in the stats panel.",
            "##CCAnnouncePotMsg",
            () => chat.AnnouncePotMessage,
            v  => { chat.AnnouncePotMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Request Gil",
            "Button: 'Request Gil' in the game tab.",
            "##CCTellAmountMsg",
            () => chat.TellAmountRequestMessage,
            v  => { chat.TellAmountRequestMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Request Gil (Buyer)",
            "Button: 'Request Gil (Buyer)' shown when another player is paying for this player.",
            "##CCTellBuyerMsg",
            () => chat.RequestGilBuyerMessage,
            v  => { chat.RequestGilBuyerMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Payment Received",
            "Sent when Auto Begin on Payment starts the turn, telling the player they may roll.",
            "##CCPaymentReceivedMsg",
            () => chat.PaymentReceivedMessage,
            v  => { chat.PaymentReceivedMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Wrong Roll",
            "Button: 'Send Correction' on the Current Roll card when a player rolls out of the wrong number.",
            "##CCWrongRollMsg",
            () => chat.WrongRollMessage,
            v  => { chat.WrongRollMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Ask to Roll",
            "Button: 'Ask to Roll' shown while waiting for the player's opening dice roll.",
            "##CCAskRollMsg",
            () => chat.AskRollMessage,
            v  => { chat.AskRollMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Ask to Roll (With Coins)",
            "Button: 'Ask to Roll' used instead once the player has collected at least one coin.",
            "##CCAskRollCoinsMsg",
            () => chat.AskRollWithCoinsMessage,
            v  => { chat.AskRollWithCoinsMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Win Shout",
            "Button: 'Announce Winner' on the session winner screen.",
            "##CCWinMsg",
            () => chat.WinShoutMessage,
            v  => { chat.WinShoutMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Score",
            "Button: 'Announce Score' when a player's turn ends and they are NOT currently leading.",
            "##CCLossUnluckyMsg",
            () => chat.LossUnluckyMessage,
            v  => { chat.LossUnluckyMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Lead",
            "Button: 'Announce Lead' when a player's turn ends and they ARE currently in the lead.",
            "##CCLossWinningMsg",
            () => chat.LossWinningMessage,
            v  => { chat.LossWinningMessage = v; this.config.Save(); });
    }

    private void DrawAutoSection()
    {
        var chat = this.config.CoinCollector.Chat;

        var askRoll = chat.AutoSendAskRoll;
        if (SettingToggle.Draw("Auto Ask to Roll", "##CCAutoAskRoll",
                "fires automatically after each roll with the next dice number", ref askRoll))
        {
            chat.AutoSendAskRoll = askRoll;
            this.config.Save();
        }
        ImGui.Spacing();

        var loss = chat.AutoSendLoss;
        if (SettingToggle.Draw("Auto Announce Score/Lead", "##CCAutoLoss",
                "auto-sends Announce Score or Announce Lead when a player busts", ref loss))
        {
            chat.AutoSendLoss = loss;
            this.config.Save();
        }
        ImGui.Spacing();

        var wrongRoll = chat.AutoSendWrongRoll;
        if (SettingToggle.Draw("Auto Send Wrong Roll", "##CCAutoWrongRoll",
                "posts the correction message when a player rolls out of the wrong number", ref wrongRoll))
        {
            chat.AutoSendWrongRoll = wrongRoll;
            this.config.Save();
        }
        ImGui.Spacing();

        var payment = chat.AutoSendPaymentReceived;
        if (SettingToggle.Draw("Auto Send Payment Received", "##CCAutoPaymentReceived",
                "sent when Auto Begin on Payment starts the turn; that toggle lives under Settings", ref payment))
        {
            chat.AutoSendPaymentReceived = payment;
            this.config.Save();
        }
        ImGui.Spacing();

        var names = chat.LeaderboardNamesInMessage;
        if (SettingToggle.DrawIntField("Names in {leaderboard} Messages", "##CCLeaderboardNames",
                "How many ranked players the {leaderboard} placeholder lists before summarising.",
                ref names, 1, 20, 1, NameCapFieldWidth))
        {
            chat.LeaderboardNamesInMessage = names;
            this.config.Save();
        }
    }

    private static void DrawMessageField(string label, string hint, string id, Func<string> get, Action<string> set)
    {
        ImGui.TextUnformatted(label);
        ImGui.TextDisabled(hint);
        var val = get();
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText(id, ref val, 256))
            set(val);
    }

    private static void DrawMultilineMessageField(string label, string hint, string id, Func<string> get, Action<string> set)
    {
        ImGui.TextUnformatted(label);
        ImGui.TextDisabled(hint);
        var val = get();
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputTextMultiline(id, ref val, 1024, new Vector2(-1f, ImGui.GetTextLineHeight() * 6f)))
            set(val);
    }
}
