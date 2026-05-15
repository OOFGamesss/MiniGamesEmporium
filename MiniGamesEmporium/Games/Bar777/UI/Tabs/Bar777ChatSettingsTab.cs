using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Draws the Chat settings tab for BAR 777, allowing the host to configure and preview all automated and manually triggered message templates along with their placeholder reference.</summary>

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
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "BAR 777");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Chat Settings");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("All placeholders work in every message field:");
        ImGui.TextDisabled("  {player}    = player name with @World (e.g. Amari Tempest@Omega)");
        ImGui.TextDisabled("  {position}  = player's position in the waiting list");
        ImGui.TextDisabled("  {cost}      = configured entry cost in Gil");
        ImGui.TextDisabled("  {rolls}     = configured roll count");
        ImGui.TextDisabled("  {remaining} = rolls remaining (or Gil owed in the Amount Request message)");
        ImGui.TextDisabled("  {totalpot}  = total pot (boosted pot + trades)");
        ImGui.Spacing();
        DrawHybridMessageSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawManualMessageSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawAutoMessageSection();
    }
    private void DrawHybridMessageSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Hybrid Messages");
        ImGui.Spacing();
        var autoStartRolls = this.config.Bar777.Chat.AutoStartRolls;
        if (ImGui.Checkbox("Auto Start Rolls##AutoStartRollsToggle", ref autoStartRolls))
        {
            this.config.Bar777.Chat.AutoStartRolls = autoStartRolls;
            this.config.Save();
        }
        ImGui.TextDisabled(autoStartRolls
            ? "Auto-sent when payment is verified. The manual button is hidden on the Game panel."
            : "Sent via the 'Send Start Rolls Msg' button on the Game panel.");
        {
            var val = this.config.Bar777.Chat.StartRollsMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##StartRollsMsg", ref val, 256))
            {
                this.config.Bar777.Chat.StartRollsMessage = val;
                this.config.Save();
            }
        }
    }
    private void DrawManualMessageSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Manual Trigger Messages");
        ImGui.Spacing();
        ImGui.TextUnformatted("/tell Amount Request");
        ImGui.TextDisabled("Sent by the '/tell Amount Request' button. {remaining} = gil still owed.");
        {
            var val = this.config.Bar777.Chat.TellAmountRequestMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##TellAmtMsg", ref val, 256))
            {
                this.config.Bar777.Chat.TellAmountRequestMessage = val;
                this.config.Save();
            }
        }
        ImGui.Spacing();
        ImGui.TextUnformatted("/yell Pot");
        ImGui.TextDisabled("Sent by the '/yell Pot' button on the stats panel. {totalpot} = total pot.");
        {
            var val = this.config.Bar777.Chat.YellPotMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##YellPotMsg", ref val, 256))
            {
                this.config.Bar777.Chat.YellPotMessage = val;
                this.config.Save();
            }
        }
        ImGui.Spacing();
        ImGui.TextUnformatted("Announce keyword");
        ImGui.TextDisabled("Sent by the 'Announce Keyword' button in the queue panel. {keyword} = the configured join keyword.");
        {
            var val = this.config.Bar777.Chat.AnnounceKeywordMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##AnnounceKeywordMsg", ref val, 256))
            {
                this.config.Bar777.Chat.AnnounceKeywordMessage = val;
                this.config.Save();
            }
        }
    }
    private void DrawAutoMessageSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Auto Messages");
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendHalfway;
            if (ImGui.Checkbox("Auto Halfway##HalfwayToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendHalfway = toggle;
                this.config.Save();
            }
        }
        ImGui.TextDisabled("Auto-sent when half the rolls are used. {remaining} = rolls left.");
        {
            var val = this.config.Bar777.Chat.HalfwayMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##HalfwayMsg", ref val, 256))
            {
                this.config.Bar777.Chat.HalfwayMessage = val;
                this.config.Save();
            }
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendUnlucky;
            if (ImGui.Checkbox("Auto Unlucky##UnluckyToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendUnlucky = toggle;
                this.config.Save();
            }
        }
        ImGui.TextDisabled("Auto-sent when all rolls are used without a win.");
        {
            var val = this.config.Bar777.Chat.UnluckyMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##UnluckyMsg", ref val, 256))
            {
                this.config.Bar777.Chat.UnluckyMessage = val;
                this.config.Save();
            }
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendWinShout;
            if (ImGui.Checkbox("Auto Win Shout##WinToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendWinShout = toggle;
                this.config.Save();
            }
        }
        ImGui.TextDisabled("Auto-sent when a win is detected. {totalpot} = total pot.");
        {
            var val = this.config.Bar777.Chat.WinShoutMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##WinShoutMsg", ref val, 256))
            {
                this.config.Bar777.Chat.WinShoutMessage = val;
                this.config.Save();
            }
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendJoinQueue;
            if (ImGui.Checkbox("Auto Join Queue Message##JoinQueueToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendJoinQueue = toggle;
                this.config.Save();
            }
        }
        ImGui.TextDisabled("Auto-sent via /tell when a player successfully joins the queue. Only fires if");
        ImGui.TextDisabled("they were not already in the queue. {player} = player name, {position} = their queue number.");
        {
            var val = this.config.Bar777.Chat.JoinQueueMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##JoinQueueMsg", ref val, 256))
            {
                this.config.Bar777.Chat.JoinQueueMessage = val;
                this.config.Save();
            }
        }
        ImGui.Spacing();
        {
            var toggle = this.config.Bar777.Chat.AutoSendReminderToPlay;
            if (ImGui.Checkbox("Auto Reminder to Play##ReminderToggle", ref toggle))
            {
                this.config.Bar777.Chat.AutoSendReminderToPlay = toggle;
                this.config.Save();
            }
        }
        ImGui.TextDisabled("Auto-sent via /tell when a player's queue position reaches the threshold or below.");
        ImGui.TextDisabled("Sends once per player per session. A manual resend button appears in the queue list.");
        ImGui.TextDisabled("{player} = player name.");
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
        ImGui.TextDisabled("Players at this position or closer to the front will receive the reminder.");
        {
            var val = this.config.Bar777.Chat.ReminderToPlayMessage;
            ImGui.SetNextItemWidth(-1f);
            if (ImGui.InputText("##ReminderToPlayMsg", ref val, 256))
            {
                this.config.Bar777.Chat.ReminderToPlayMessage = val;
                this.config.Save();
            }
        }
    }
}
