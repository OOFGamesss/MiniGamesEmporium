using Dalamud.Bindings.ImGui;
using ECommons.ImGuiMethods;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Renders the betting toggle, bet unit and bet keyword fields shared by the start door and settings tab.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Components;
public static class DeathrollBettingFields
{
    public static void Draw(PluginConfiguration config, string imguiSuffix, float fieldWidth)
    {
        var cfg = config.DeathrollTournament;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Betting");
        ImGui.Spacing();
        var enabled = cfg.BettingEnabled;
        if (ImGui.Checkbox($"Enable Betting##DRBetEnabled_{imguiSuffix}", ref enabled))
        {
            cfg.BettingEnabled = enabled;
            config.Save();
        }
        if (!enabled) return;
        ImGui.Spacing();
        ImGui.Indent();
        var betUnit = (int)cfg.BetUnit;
        ImGui.TextDisabled("Bet Unit (Gil)");
        ImGui.SetNextItemWidth(fieldWidth);
        if (ImGuiEx.InputFancyNumeric($"##DRBetUnit_{imguiSuffix}", ref betUnit, 100_000))
        {
            cfg.BetUnit = Math.Max(0, betUnit);
            config.Save();
        }
        ImGui.Spacing();
        var autoBet = cfg.AutoBetKeyword;
        if (ImGui.Checkbox($"Auto Bet Keyword##DRAutoBet_{imguiSuffix}", ref autoBet))
        {
            cfg.AutoBetKeyword = autoBet;
            config.Save();
        }
        if (autoBet)
        {
            ImGui.Spacing();
            ImGui.Indent();
            var keyword = cfg.BetKeyword;
            ImGui.TextDisabled("Bet keyword (followed by the target's name)");
            ImGui.SetNextItemWidth(fieldWidth);
            if (ImGui.InputText($"##DRBetKeyword_{imguiSuffix}", ref keyword, 32))
            {
                cfg.BetKeyword = keyword;
                config.Save();
            }
            ImGui.Spacing();
            ImGui.TextDisabled("Listen on chats");
            ImGui.SetNextItemWidth(fieldWidth);
            if (QueueChannelCombo.Draw($"DRBetListen_{imguiSuffix}", cfg.BetChannels))
                config.Save();
            ImGui.Unindent();
        }
        ImGui.Unindent();
    }
}
