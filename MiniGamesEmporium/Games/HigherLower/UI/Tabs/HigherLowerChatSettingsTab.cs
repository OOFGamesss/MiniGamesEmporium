using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Chat settings tab for Higher/Lower message templates.</summary>

namespace MiniGamesEmporium.Games.HigherLower.UI.Tabs;
public sealed class HigherLowerChatSettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.HigherLowerOrange;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;

    public HigherLowerChatSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Higher/Lower");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        this.card.Draw("##HLChatPlaceholders", "Available Placeholders", CardAccent, CardTitle, DrawPlaceholderReference);
        this.card.Draw("##HLChatManual", "Manual Trigger Messages", CardAccent, CardTitle, DrawManualSection);
        this.card.Draw("##HLChatAuto", "Auto-Send Toggles", CardAccent, CardTitle, DrawAutoSection);
    }

    private static readonly (string Token, string Desc)[] Placeholders =
    [
        ("{player}",       "Player name; @World included only for /tell messages"),
        ("{buyername}",    "Name of the player paying on someone else's behalf (Request Gil (Buyer) message only)"),
        ("{cost}",         "Entry cost in Gil"),
        ("{rounds}",       "Number of rounds correct"),
        ("{totalpot}",     "Total pot in Gil"),
        ("{winningamount}", "Winner's share of the pot (Win Shout message only)"),
        ("{rollednumber}", "Most recently rolled number (Ask Guess message only)"),
        ("{highestround}", "Target rounds to lead (or player's own rounds when winning) - Announce messages only"),
    ];

    private static void DrawPlaceholderReference() => PlaceholderReference.Draw(Placeholders);

    private void DrawManualSection()
    {
        var chat = this.config.HigherLower.Chat;
        DrawMessageField(
            "Advertise",
            "Button: 'Advertise' on the top-left of the session control bar. Shouted to the zone to pull in new players.",
            "##HLAdvertiseMsg",
            () => chat.AdvertiseMessage,
            v  => { chat.AdvertiseMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMultilineMessageField(
            "Rules",
            "Button: 'Send Rules' on the top-left of the session control bar. Each line is sent as its own message.",
            "##HLRulesMsg",
            () => chat.RulesMessage,
            v  => { chat.RulesMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Pot",
            "Button: 'Announce Pot' in the stats panel.",
            "##HLAnnouncePotMsg",
            () => chat.AnnouncePotMessage,
            v  => { chat.AnnouncePotMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Request Gil",
            "Button: 'Request Gil' in the game tab.",
            "##HLTellAmountMsg",
            () => chat.TellAmountRequestMessage,
            v  => { chat.TellAmountRequestMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Request Gil (Buyer)",
            "Button: 'Request Gil (Buyer)' shown when another player is paying for this player.",
            "##HLTellBuyerMsg",
            () => chat.RequestGilBuyerMessage,
            v  => { chat.RequestGilBuyerMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Let's Play",
            "Button: 'Let's Play' shown after payment is verified. Also auto-sent on payment (see toggles).",
            "##HLLetsPlayMsg",
            () => chat.LetsPlayMessage,
            v  => { chat.LetsPlayMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Ask Guess",
            "Button: 'Ask Guess' shown after each roll while waiting for the player's guess.",
            "##HLAskGuessMsg",
            () => chat.AskGuessMessage,
            v  => { chat.AskGuessMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Win Shout",
            "Button: 'Announce Winner' on the session winner screen.",
            "##HLWinMsg",
            () => chat.WinShoutMessage,
            v  => { chat.WinShoutMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Score",
            "Button: 'Announce Score' when player's turn ends and they are NOT currently leading.",
            "##HLLossUnluckyMsg",
            () => chat.LossUnluckyMessage,
            v  => { chat.LossUnluckyMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Lead",
            "Button: 'Announce Lead' when player's turn ends and they ARE currently in the lead.",
            "##HLLossWinningMsg",
            () => chat.LossWinningMessage,
            v  => { chat.LossWinningMessage = v; this.config.Save(); });
    }

    private void DrawAutoSection()
    {
        var chat = this.config.HigherLower.Chat;
        {
            var toggle = chat.AutoSendLetsPlay;
            if (ImGui.Checkbox("Auto Let's Play##HLAutoLetsPlay", ref toggle))
            {
                chat.AutoSendLetsPlay = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when payment is verified (or bypassed)");
        }
        ImGui.Spacing();
        {
            var toggle = chat.AutoSendAskGuess;
            if (ImGui.Checkbox("Auto Ask Guess##HLAutoAskGuess", ref toggle))
            {
                chat.AutoSendAskGuess = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically after each roll while awaiting the player's guess");
        }
        ImGui.Spacing();
        {
            var toggle = chat.AutoSendLoss;
            if (ImGui.Checkbox("Auto Announce Loss/Lead##HLAutoLoss", ref toggle))
            {
                chat.AutoSendLoss = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- auto-sends Announce Loss or Announce Lead message when a player loses");
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
