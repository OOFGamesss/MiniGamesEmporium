using System;
using Dalamud.Bindings.ImGui;
using ECommons.ImGuiMethods;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Renders the Coin Collector pre-session settings fields shared by the door and settings tab.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.UI.Components;
public static class CoinCollectorPreSessionSettingsFields
{
    public static void Draw(PluginConfiguration config)
    {
        DrawEntryCost(config);
        DrawBoostedPot(config);
        DrawStartingRollMax(config);
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
        var cost = config.CoinCollector.EntryCost;
        ImGui.TextDisabled("Entry Cost (Gil)");
        ImGui.SetNextItemWidth(FieldWidth());
        if (ImGuiEx.InputFancyNumeric("##CCEntryCost", ref cost, 10_000))
        {
            config.CoinCollector.EntryCost = Math.Max(0, cost);
            config.Save();
        }
    }

    private static void DrawBoostedPot(PluginConfiguration config)
    {
        var pot = (int)config.CoinCollector.BoostedPot;
        ImGui.TextDisabled("Boosted Pot (Gil)");
        ImGui.SetNextItemWidth(FieldWidth());
        if (ImGuiEx.InputFancyNumeric("##CCBoostedPot", ref pot, 100_000))
        {
            config.CoinCollector.BoostedPot = (long)Math.Max(0, pot);
            config.Save();
        }
    }

    private static void DrawStartingRollMax(PluginConfiguration config)
    {
        var start = config.CoinCollector.StartingRollMax;
        ImGui.TextDisabled("Starting Roll Max");
        ImGui.SetNextItemWidth(FieldWidth());
        if (ImGuiEx.InputFancyNumeric("##CCStartingRollMax", ref start, 1))
        {
            config.CoinCollector.StartingRollMax = Math.Clamp(start, 2, 999);
            config.Save();
        }
        ImGui.TextDisabled("Leave at 999 for a plain /dice opening roll.");
    }

    private static void DrawAutoWinCount(PluginConfiguration config)
    {
        ImGui.Spacing();
        ImGui.TextDisabled("Auto Win Count");
        if (ImGui.RadioButton("Yes##CCAutoWinYes", config.CoinCollector.AutoWinCount))
        {
            config.CoinCollector.AutoWinCount = true;
            config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("No##CCAutoWinNo", !config.CoinCollector.AutoWinCount))
        {
            config.CoinCollector.AutoWinCount = false;
            config.Save();
        }
        if (config.CoinCollector.AutoWinCount)
        {
            ImGui.Spacing();
            var target = config.CoinCollector.TargetCoins;
            ImGui.TextDisabled("Winning Coin Count");
            ImGui.SetNextItemWidth(FieldWidth());
            if (ImGuiEx.InputFancyNumeric("##CCTargetCoins", ref target, 1))
            {
                config.CoinCollector.TargetCoins = Math.Clamp(target, 1, 100);
                config.Save();
            }
        }
    }

    private static void DrawMultipleWinnersToggle(PluginConfiguration config)
    {
        ImGui.Spacing();
        ImGui.TextDisabled("Allow Multiple Winners");
        if (ImGui.RadioButton("Yes##CCMultiWin", config.CoinCollector.AllowMultipleWinners))
        {
            config.CoinCollector.AllowMultipleWinners = true;
            config.Save();
        }
        ImGui.SameLine();
        if (ImGui.RadioButton("No##CCNoMultiWin", !config.CoinCollector.AllowMultipleWinners))
        {
            config.CoinCollector.AllowMultipleWinners = false;
            config.Save();
        }
        if (config.CoinCollector.AllowMultipleWinners)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("- winners share the pot equally");
        }
    }

    private static void DrawTradesToPotPercentSetting(PluginConfiguration config)
    {
        ImGui.Spacing();
        var pct = config.CoinCollector.TradesToPotPercent;
        ImGui.TextDisabled("Trades to Pot (%)");
        ImGui.SetNextItemWidth(FieldWidth());
        if (ImGui.SliderInt("##CCTradesToPotPercent", ref pct, 0, 100, "%d%%"))
        {
            config.CoinCollector.TradesToPotPercent = Math.Clamp(pct, 0, 100);
            config.Save();
        }
    }
}
