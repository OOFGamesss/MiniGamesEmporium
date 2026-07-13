using System;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Chat settings tab for Higher/Lower message templates.</summary>

namespace MiniGamesEmporium.Games.HigherLower.UI.Tabs;
public sealed class HigherLowerChatSettingsTab
{
    private readonly PluginConfiguration config;

    public HigherLowerChatSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.HigherLowerOrange, "Higher/Lower");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        DrawPlaceholderReference();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawManualSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawAutoSection();
    }

    private static readonly (string Token, string Desc)[] Placeholders =
    [
        ("{player}",       "Player name; @World included only for /tell messages"),
        ("{cost}",         "Entry cost in Gil"),
        ("{rounds}",       "Number of rounds correct"),
        ("{totalpot}",     "Total pot in Gil"),
        ("{winningamount}", "Winner's share of the pot (Win Shout message only)"),
        ("{rollednumber}", "Most recently rolled number (Ask Guess message only)"),
        ("{highestround}", "Target rounds to lead (or player's own rounds when winning) - Announce messages only"),
    ];

    private static void DrawPlaceholderReference()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Available Placeholders");
        ImGui.Spacing();
        var descColX = Placeholders.Max(p => ImGui.CalcTextSize(p.Token).X) + 20f;
        foreach (var (token, desc) in Placeholders)
            DrawPlaceholderRow(token, desc, descColX);
    }

    private static void DrawPlaceholderRow(string token, string desc, float descColX)
    {
        ImGui.TextColored(new Vector4(1f, 0.80f, 0.30f, 1f), token);
        ImGui.SameLine(descColX);
        ImGui.TextDisabled(desc);
    }

    private void DrawManualSection()
    {
        var chat = this.config.HigherLower.Chat;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Manual Trigger Messages");
        ImGui.Spacing();
        DrawMultilineMessageField(
            "Rules",
            "Button: 'Send Rules' on the top-left of the Game panel.",
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
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Auto-Send Toggles");
        ImGui.Spacing();
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
