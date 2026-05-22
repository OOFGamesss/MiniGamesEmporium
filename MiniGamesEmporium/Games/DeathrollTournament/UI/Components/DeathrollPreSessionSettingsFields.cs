using Dalamud.Bindings.ImGui;
using ECommons.ImGuiMethods;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Renders the entry cost and boosted pot Gil fields shown on the Deathroll Tournament start door.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Components;
public static class DeathrollPreSessionSettingsFields
{
    public static void Draw(PluginConfiguration config)
    {
        DrawEntryCost(config);
        DrawBoostedPot(config);
    }

    private static void DrawEntryCost(PluginConfiguration config)
    {
        var cost = (int)config.DeathrollTournament.EntryCost;
        ImGui.TextDisabled("Entry Cost (Gil)");
        ImGui.SetNextItemWidth(ResolvedWidth());
        if (ImGuiEx.InputFancyNumeric("##DREntryCost", ref cost, 100_000))
        {
            config.DeathrollTournament.EntryCost = Math.Max(0, cost);
            config.Save();
        }
    }

    private static void DrawBoostedPot(PluginConfiguration config)
    {
        var pot = (int)config.DeathrollTournament.BoostedPot;
        ImGui.TextDisabled("Boosted Pot (Gil)");
        ImGui.SetNextItemWidth(ResolvedWidth());
        if (ImGuiEx.InputFancyNumeric("##DRBoostedPot", ref pot, 100_000))
        {
            config.DeathrollTournament.BoostedPot = Math.Max(0, pot);
            config.Save();
        }
    }

    private static float ResolvedWidth()
    {
        var w = ImGui.GetContentRegionAvail().X;
        if (w <= 48f) return EmporiumNeonTheme.StartDoorPanelWidth;
        return MathF.Max(80f, w - ImGui.GetFrameHeight() * 2f - 2f);
    }
}
