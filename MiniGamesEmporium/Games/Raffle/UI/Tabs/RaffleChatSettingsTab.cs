using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Draws the Chat settings tab for Raffle message templates and auto-send toggles.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Tabs;
public sealed class RaffleChatSettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.RaffleTeal;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;

    public RaffleChatSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Raffle");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        this.card.Draw("##RaffleChatPlaceholders", "Available Placeholders", CardAccent, CardTitle, DrawPlaceholderReference);
        this.card.Draw("##RaffleChatTemplates", "Message Templates", CardAccent, CardTitle, DrawMessageFields);
        this.card.Draw("##RaffleChatAuto", "Auto-Send Toggles", CardAccent, CardTitle, DrawAutoSection);
    }

    private static readonly (string Token, string Desc)[] Placeholders =
    [
        ("{ticketcost}",    "Ticket cost in Gil"),
        ("{totalpot}",      "Total pot in Gil"),
        ("{boostedpot}",    "Boosted pot amount in Gil"),
        ("{ticketssold}",   "Number of tickets sold so far"),
        ("{maxtickets}",    "Maximum tickets per player"),
        ("{closetime}",     "Closing time as HH:mm Server Time"),
        ("{timeleft}",      "Time remaining until close"),
        ("{keyword}",       "The auto-join keyword"),
        ("{winner}",        "The drawn winner's name"),
        ("{winningnumber}", "The winning ticket number"),
        ("{player}",        "Registered player name (request messages)"),
        ("{buyername}",     "The buyer's full name including @World (buyer request only)"),
        ("{numbers}",       "The player's ticket numbers (ticket numbers tell only)"),
    ];

    private static void DrawPlaceholderReference() => PlaceholderReference.Draw(Placeholders);

    private void DrawMessageFields()
    {
        var chat = this.config.Raffle.Chat;
        DrawMessageField("Advertise", "Button: 'Advertise' on the top-left of the session control bar. Shouted to the zone to pull in new ticket buyers.",
            "##RaffleAdvertiseMsg", () => chat.AdvertiseMessage, v => { chat.AdvertiseMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMultilineMessageField("Rules", "Button: 'Send Rules' on the top-left of the session control bar. Each line is sent as its own message.",
            "##RaffleRulesMsg", () => chat.RulesMessage, v => { chat.RulesMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Announce Pot", "Button: 'Announce Pot' in the game panel.",
            "##RaffleAnnPotMsg", () => chat.AnnouncePotMessage, v => { chat.AnnouncePotMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Announce Tickets Sold", "Button: 'Tickets Sold' in the game panel.",
            "##RaffleAnnTicketsMsg", () => chat.AnnounceTicketsSoldMessage, v => { chat.AnnounceTicketsSoldMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Announce Closing Time", "Button: 'Closing Time' in the game panel.",
            "##RaffleAnnClosingMsg", () => chat.AnnounceClosingTimeMessage, v => { chat.AnnounceClosingTimeMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Join Reminder", "Button: 'Join Reminder' in the game panel.",
            "##RaffleAnnJoinMsg", () => chat.AnnounceJoinReminderMessage, v => { chat.AnnounceJoinReminderMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Raffle Closed", "Auto-sent when 'Raffle Closed' is clicked. Announces the draw is closed, tickets sold and total pot.",
            "##RaffleAnnClosedMsg", () => chat.AnnounceRaffleClosedMessage, v => { chat.AnnounceRaffleClosedMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Announce Winner", "Button: 'Announce Winner' after the draw. Also used for auto-announce below.",
            "##RaffleAnnWinnerMsg", () => chat.AnnounceWinnerMessage, v => { chat.AnnounceWinnerMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Request Gil", "Button: 'Request' per unpaid player. {player} includes the world for cross-world /tell.",
            "##RaffleReqGilMsg", () => chat.RequestGilMessage, v => { chat.RequestGilMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Request Gil (Buyer)", "Button: 'Request Gil (Buyer)' in the buyer popup.",
            "##RaffleReqGilBuyerMsg", () => chat.RequestGilBuyerMessage, v => { chat.RequestGilBuyerMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField("Ticket Numbers", "Button: 'Send Numbers' per player who holds tickets. {numbers} lists their numbers.",
            "##RaffleTicketNumbersMsg", () => chat.TicketNumbersMessage, v => { chat.TicketNumbersMessage = v; this.config.Save(); });
    }

    private void DrawAutoSection()
    {
        var toggle = this.config.Raffle.Chat.AutoAnnounceWinner;
        if (ImGui.Checkbox("Auto Announce Winner##RaffleAutoWinner", ref toggle))
        {
            this.config.Raffle.Chat.AutoAnnounceWinner = toggle;
            this.config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("- fires automatically when a winning ticket is drawn to a player");

        var autoNumbers = this.config.Raffle.Chat.AutoSendTicketNumbers;
        if (ImGui.Checkbox("Auto Send Ticket Numbers##RaffleAutoNumbers", ref autoNumbers))
        {
            this.config.Raffle.Chat.AutoSendTicketNumbers = autoNumbers;
            this.config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("- tells a registered player their numbers when a trade grants them tickets");
        if (autoNumbers && this.config.Raffle.ShuffleTicketNumbers)
        {
            ImGui.Indent();
            ImGui.TextDisabled("Shuffle mode: held back until you close the raffle, then use 'Send All Numbers'.");
            ImGui.Unindent();
        }
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

    private static void DrawMessageField(string label, string hint, string id, Func<string> get, Action<string> set)
    {
        ImGui.TextUnformatted(label);
        ImGui.TextDisabled(hint);
        var val = get();
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText(id, ref val, 256))
            set(val);
    }
}
