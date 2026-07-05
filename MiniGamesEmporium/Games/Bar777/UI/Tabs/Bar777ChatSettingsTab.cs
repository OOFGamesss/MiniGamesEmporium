using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Draws the Chat settings tab for BAR 777 message templates.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Tabs;
public sealed class Bar777ChatSettingsTab
{
    private readonly PluginConfiguration config;
    public Bar777ChatSettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }
    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, this.config.Bar777.CustomName);
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        DrawChannelSelector();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawPlaceholderReference();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawManualMessageSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawAutoMessageSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawAutoSendTogglesSection();
    }
    private void DrawChannelSelector()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Chat Channel");
        ImGui.TextDisabled("Changes messages set for /say, /party and /alliance to a channel of your choice");
        ImGui.Spacing();
        var channel = this.config.Bar777.Chat.Channel;
        if (ImGui.RadioButton("Say##ChannelSay", channel == ChatChannel.Say))
        {
            this.config.Bar777.Chat.ApplyChatChannel(ChatChannel.Say);
            this.config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Party##ChannelParty", channel == ChatChannel.Party))
        {
            this.config.Bar777.Chat.ApplyChatChannel(ChatChannel.Party);
            this.config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Alliance##ChannelAlliance", channel == ChatChannel.Alliance))
        {
            this.config.Bar777.Chat.ApplyChatChannel(ChatChannel.Alliance);
            this.config.Save();
        }
    }
    private static void DrawPlaceholderReference()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Available Placeholders");
        ImGui.Spacing();
        DrawPlaceholderRow("{player}",      "Player name; @World included only for /tell messages (e.g. John Doe@Omega)");
        DrawPlaceholderRow("{buyername}",   "Buyer's full name always including @World (e.g. Jane Doe@Omega) - buyer request message only");
        DrawPlaceholderRow("{position}",    "Player's position in the waiting list");
        DrawPlaceholderRow("{cost}",        "Cost per roll in Gil");
        DrawPlaceholderRow("{maxcost}",     "Cost per roll × max rolls");
        DrawPlaceholderRow("{rolls}",       "Max rolls allowed per session");
        DrawPlaceholderRow("{boughtrolls}", "Rolls this player actually purchased");
        DrawPlaceholderRow("{remaining}",   "Rolls remaining");
        DrawPlaceholderRow("{totalpot}",    "Total pot in Gil");
        DrawPlaceholderRow("{keyword}",     "Queue join keyword");
    }

    private static void DrawPlaceholderRow(string token, string desc)
    {
        ImGui.TextColored(new Vector4(1f, 0.80f, 0.30f, 1f), token);
        ImGui.SameLine(110f);
        ImGui.TextDisabled(desc);
    }

    private void DrawManualMessageSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Manual Trigger Messages");
        ImGui.Spacing();
        DrawMultilineMessageField(
            "Rules",
            "Button: 'Send Rules' on the top-left of the Game panel.",
            "##RulesMsg",
            () => this.config.Bar777.Chat.RulesMessage,
            v => { this.config.Bar777.Chat.RulesMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Start Rolls",
            "Button: 'Send Start Rolls Msg' on the Game panel when auto-start is off. Also used for auto-start below.",
            "##StartRollsMsg",
            () => this.config.Bar777.Chat.StartRollsMessage,
            v => { this.config.Bar777.Chat.StartRollsMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Request Gil",
            "Button: 'Request Gil'.",
            "##TellAmtMsg",
            () => this.config.Bar777.Chat.TellAmountRequestMessage,
            v => { this.config.Bar777.Chat.TellAmountRequestMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Request Gil (Buyer)",
            "Button: 'Request Gil (Buyer)' - sent to the buyer paying for another player. Use {buyername} for the buyer and {player} for who they are paying for.",
            "##TellBuyerMsg",
            () => this.config.Bar777.Chat.TellBuyerRequestMessage,
            v => { this.config.Bar777.Chat.TellBuyerRequestMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce Pot",
            "Button: 'Announce Pot' on the stats panel.",
            "##YellPotMsg",
            () => this.config.Bar777.Chat.YellPotMessage,
            v => { this.config.Bar777.Chat.YellPotMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Announce keyword",
            "Button: 'Announce Keyword' in the queue panel.",
            "##AnnounceKeywordMsg",
            () => this.config.Bar777.Chat.AnnounceKeywordMessage,
            v => { this.config.Bar777.Chat.AnnounceKeywordMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Next Player Up",
            "Button: 'Next Player Up' next to the player name on the Game tab (queue mode only).",
            "##NextPlayerUpMsg",
            () => this.config.Bar777.Chat.NextPlayerUpMessage,
            v => { this.config.Bar777.Chat.NextPlayerUpMessage = v; this.config.Save(); });
    }
    private void DrawAutoMessageSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Auto Messages");
        ImGui.Spacing();
        DrawMessageField(
            "Halfway",
            "Auto-sent when half the rolls are used.",
            "##HalfwayMsg",
            () => this.config.Bar777.Chat.HalfwayMessage,
            v => { this.config.Bar777.Chat.HalfwayMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Unlucky",
            "Auto-sent when all rolls are used without a win.",
            "##UnluckyMsg",
            () => this.config.Bar777.Chat.UnluckyMessage,
            v => { this.config.Bar777.Chat.UnluckyMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Win Shout",
            "Auto-sent when a win is detected.",
            "##WinShoutMsg",
            () => this.config.Bar777.Chat.WinShoutMessage,
            v => { this.config.Bar777.Chat.WinShoutMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Join Queue",
            "Auto-sent via /tell when a player successfully joins the queue (not if already queued).",
            "##JoinQueueMsg",
            () => this.config.Bar777.Chat.JoinQueueMessage,
            v => { this.config.Bar777.Chat.JoinQueueMessage = v; this.config.Save(); });
        ImGui.Spacing();
        DrawMessageField(
            "Reminder to Play",
            "Auto-sent via /tell when a player's queue position reaches the threshold or below. Sends once per player per session.",
            "##ReminderToPlayMsg",
            () => this.config.Bar777.Chat.ReminderToPlayMessage,
            v => { this.config.Bar777.Chat.ReminderToPlayMessage = v; this.config.Save(); });
    }
    private void DrawAutoSendTogglesSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Auto-Send Toggles");
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoStartRolls;
            if (ImGui.Checkbox("Auto Start Rolls##AutoStartRollsToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoStartRolls = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when payment is verified; hides the manual button on the Game panel");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendHalfway;
            if (ImGui.Checkbox("Auto Halfway##HalfwayToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendHalfway = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when half the rolls are used");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendUnlucky;
            if (ImGui.Checkbox("Auto Unlucky##UnluckyToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendUnlucky = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when all rolls are used without a win");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendWinShout;
            if (ImGui.Checkbox("Auto Win Shout##WinToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendWinShout = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when a win is detected");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendReminderToPlay;
            if (ImGui.Checkbox("Auto Reminder to Play##ReminderToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendReminderToPlay = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically when queue position reaches the threshold; manual resend available in queue list");
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendJoinQueue;
            if (ImGui.Checkbox("Auto Join Queue Message##JoinQueueToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendJoinQueue = toggle;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("- fires automatically via /tell when a player joins the queue");
        }
        ImGui.Spacing();
        ImGui.TextUnformatted("Queue position threshold:");
        {
            var threshold = this.config.Bar777.Chat.ReminderQueueThreshold;
            ImGui.SetNextItemWidth(120f);
            if (ImGui.InputInt("##ReminderThreshold", ref threshold, 1, 1))
            {
                this.config.Bar777.Chat.ReminderQueueThreshold = Math.Max(1, threshold);
                this.config.Save();
            }
        }
        ImGui.SameLine();
        ImGui.TextDisabled("- players at this position or closer to the front receive the reminder");
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
