using System;
using Dalamud.Bindings.ImGui;
using ECommons.ImGuiMethods;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Renders the Higher/Lower pre-session settings fields shared by the door and settings tab.</summary>

namespace MiniGamesEmporium.Games.HigherLower.UI.Components;
public static class HigherLowerPreSessionSettingsFields
{
    public static void Draw(PluginConfiguration config)
    {
        DrawEntryCost(config);
        DrawBoostedPot(config);
        DrawDiceSides(config);
        DrawAutoWinCount(config);
        DrawMultipleWinnersToggle(config);
        DrawTradesToPotPercentSetting(config);
    }

    private static float FieldWidth()
    {
        var w = ImGui.GetContentRegionAvail().X;
        return w <= 48f ? EmporiumNeonTheme.StartDoorPanelWidth : MathF.Max(80f, w - ImGui.GetFrameHeight() * 2f - 2f);
    }

    private static void DrawEntryCost(PluginConfiguration config)
    {
        var cost = config.HigherLower.EntryCost;
        ImGui.TextDisabled("Entry Cost (Gil)");
        ImGui.SetNextItemWidth(FieldWidth());
        if (ImGuiEx.InputFancyNumeric("##HLEntryCost", ref cost, 10_000))
        {
            config.HigherLower.EntryCost = Math.Max(0, cost);
            config.Save();
        }
    }

    private static void DrawBoostedPot(PluginConfiguration config)
    {
        var pot = (int)config.HigherLower.BoostedPot;
        ImGui.TextDisabled("Boosted Pot (Gil)");
        ImGui.SetNextItemWidth(FieldWidth());
        if (ImGuiEx.InputFancyNumeric("##HLBoostedPot", ref pot, 100_000))
        {
            config.HigherLower.BoostedPot = (long)Math.Max(0, pot);
            config.Save();
        }
    }

    private static void DrawDiceSides(PluginConfiguration config)
    {
        var sides = config.HigherLower.DiceSides;
        ImGui.TextDisabled("Host roll /dice X");
        ImGui.SetNextItemWidth(FieldWidth());
        if (ImGuiEx.InputFancyNumeric("##HLDiceSides", ref sides, 1))
        {
            config.HigherLower.DiceSides = Math.Clamp(sides, 2, 999);
            config.Save();
        }
    }

    private static void DrawAutoWinCount(PluginConfiguration config)
    {
        ImGui.Spacing();
        ImGui.TextDisabled("Auto Win Count");
        if (ImGui.RadioButton("Yes##HLAutoWinYes", config.HigherLower.AutoWinCount))
        {
            config.HigherLower.AutoWinCount = true;
            config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("No##HLAutoWinNo", !config.HigherLower.AutoWinCount))
        {
            config.HigherLower.AutoWinCount = false;
            config.Save();
        }
        if (config.HigherLower.AutoWinCount)
        {
            ImGui.Spacing();
            var target = config.HigherLower.TargetRounds;
            ImGui.TextDisabled("Winning Round Count");
            ImGui.SetNextItemWidth(FieldWidth());
            if (ImGuiEx.InputFancyNumeric("##HLTargetRounds", ref target, 1))
            {
                config.HigherLower.TargetRounds = Math.Clamp(target, 1, 100);
                config.Save();
            }
        }
    }

    private static void DrawMultipleWinnersToggle(PluginConfiguration config)
    {
        ImGui.Spacing();
        ImGui.TextDisabled("Allow Multiple Winners");
        if (ImGui.RadioButton("Yes##HLMultiWin", config.HigherLower.AllowMultipleWinners))
        {
            config.HigherLower.AllowMultipleWinners = true;
            config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("No##HLNoMultiWin", !config.HigherLower.AllowMultipleWinners))
        {
            config.HigherLower.AllowMultipleWinners = false;
            config.Save();
        }
        if (config.HigherLower.AllowMultipleWinners)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("- winners share the pot equally");
        }
    }

    private static void DrawTradesToPotPercentSetting(PluginConfiguration config)
    {
        ImGui.Spacing();
        var pct = config.HigherLower.TradesToPotPercent;
        ImGui.TextDisabled("Trades to Pot (%)");
        ImGui.SetNextItemWidth(FieldWidth());
        if (ImGui.SliderInt("##HLTradesToPotPercent", ref pct, 0, 100, "%d%%"))
        {
            config.HigherLower.TradesToPotPercent = Math.Clamp(pct, 0, 100);
            config.Save();
        }
    }
}
