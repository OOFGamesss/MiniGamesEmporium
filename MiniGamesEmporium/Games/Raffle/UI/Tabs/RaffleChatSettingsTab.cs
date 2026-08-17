using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Draws the Chat settings tab for Raffle message templates and auto-send toggles.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Tabs;
public sealed class RaffleChatSettingsTab
{
    private readonly PluginConfiguration config;

    public RaffleChatSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.RaffleTeal, "Raffle");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        DrawPlaceholderReference();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawMessageFields();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawAutoSection();
    }

    private static void DrawPlaceholderReference()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Available Placeholders");
        ImGui.Spacing();
        DrawPlaceholderRow("{ticketcost}",    "Ticket cost in Gil");
        DrawPlaceholderRow("{totalpot}",      "Total pot in Gil");
        DrawPlaceholderRow("{boostedpot}",    "Boosted pot amount in Gil");
        DrawPlaceholderRow("{ticketssold}",   "Number of tickets sold so far");
        DrawPlaceholderRow("{maxtickets}",    "Maximum tickets per player");
        DrawPlaceholderRow("{closetime}",     "Closing time as HH:mm Server Time");
        DrawPlaceholderRow("{timeleft}",      "Time remaining until close");
        DrawPlaceholderRow("{keyword}",       "The auto-join keyword");
        DrawPlaceholderRow("{winner}",        "The drawn winner's name");
        DrawPlaceholderRow("{winningnumber}", "The winning ticket number");
        DrawPlaceholderRow("{player}",        "Registered player name (request messages)");
        DrawPlaceholderRow("{buyername}",     "The buyer's full name including @World (buyer request only)");
        DrawPlaceholderRow("{numbers}",       "The player's ticket numbers (ticket numbers tell only)");
    }

    private static void DrawPlaceholderRow(string token, string desc)
    {
        ImGui.TextColored(new Vector4(1f, 0.80f, 0.30f, 1f), token);
        ImGui.SameLine(140f);
        ImGui.TextDisabled(desc);
    }

    private void DrawMessageFields()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Message Templates");
        ImGui.Spacing();
        var chat = this.config.Raffle.Chat;
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
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Auto-Send Toggles");
        ImGui.Spacing();
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
