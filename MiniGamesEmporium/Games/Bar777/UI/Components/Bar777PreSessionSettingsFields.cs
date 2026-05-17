using Dalamud.Bindings.ImGui;
using ECommons.ImGuiMethods;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Renders the entry cost, boosted pot, roll count, winning number, and entry type fields shared between the pre-session door and the settings tab.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Components;
public static class Bar777PreSessionSettingsFields
{
    public static void Draw(PluginConfiguration config)
    {
        DrawCostSetting(config);
        DrawBoostedPotSetting(config);
        DrawRollCountSetting(config);
        DrawWinNumberSetting(config);
        DrawEntryTypeToggle(config);
    }
    private static float ResolvedFieldWidth()
    {
        var w = ImGui.GetContentRegionAvail().X;
        return w > 48f ? w : EmporiumNeonTheme.StartDoorPanelWidth;
    }
    private static void DrawCostSetting(PluginConfiguration config)
    {
        var cost = config.Bar777.CostPerRoll;
        ImGui.TextDisabled("Cost Per Roll (Gil)");
        ImGui.SetNextItemWidth(ResolvedFieldWidth());
        if (ImGuiEx.InputFancyNumeric("##Cost", ref cost, 100_000))
        {
            config.Bar777.CostPerRoll = Math.Max(0, cost);
            config.Save();
        }
    }
    private static void DrawRollCountSetting(PluginConfiguration config)
    {
        var rolls = config.Bar777.MaxRolls;
        ImGui.TextDisabled("Max Rolls");
        ImGui.SetNextItemWidth(ResolvedFieldWidth());
        if (ImGuiEx.InputFancyNumeric("##RollCount", ref rolls, 1))
        {
            config.Bar777.MaxRolls = Math.Clamp(rolls, 1, 100);
            config.Save();
        }
    }
    private static void DrawWinNumberSetting(PluginConfiguration config)
    {
        var win = config.Bar777.WinNumber;
        ImGui.TextDisabled("Winning Number");
        ImGui.SetNextItemWidth(ResolvedFieldWidth());
        if (ImGuiEx.InputFancyNumeric("##WinNumber", ref win, 1))
        {
            config.Bar777.WinNumber = Math.Clamp(win, 1, 1000);
            config.Save();
        }
    }
    private static void DrawBoostedPotSetting(PluginConfiguration config)
    {
        var pot = (int)config.Bar777.BoostedPot;
        ImGui.TextDisabled("Boosted Pot (Gil)");
        ImGui.SetNextItemWidth(ResolvedFieldWidth());
        if (ImGuiEx.InputFancyNumeric("##BoostedPot", ref pot, 100_000))
        {
            config.Bar777.BoostedPot = (long)Math.Max(0, pot);
            config.Save();
        }
    }
    private static void DrawEntryTypeToggle(PluginConfiguration config)
    {
        ImGui.Spacing();
        ImGui.TextDisabled("Entry Type");
        if (ImGui.RadioButton("Walk-in##EntryWalkIn", !config.Bar777.UseQueue))
        {
            config.Bar777.UseQueue = false;
            config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("Queue##EntryQueue", config.Bar777.UseQueue))
        {
            config.Bar777.UseQueue = true;
            config.Save();
        }
    }
}
