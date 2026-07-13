using Dalamud.Bindings.ImGui;
using ECommons.ImGuiMethods;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Renders the Raffle start-door ticket cost, max tickets, boosted pot and closing time fields.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Components;
public static class RafflePreSessionSettingsFields
{
    public static void Draw(PluginConfiguration config)
    {
        DrawTicketCost(config);
        DrawBoostedPot(config);
        DrawMaxTickets(config);
        DrawTradesToPotPercent(config);
        ImGui.Spacing();
        RaffleCloseTimeFields.DrawConfig(config, "Door", ResolvedWidth());
        ImGui.Spacing();
        RaffleAutoJoinFields.Draw(config, "Door", ResolvedWidth());
    }

    private static void DrawTicketCost(PluginConfiguration config)
    {
        var cost = (int)config.Raffle.TicketCost;
        ImGui.TextDisabled("Ticket Cost (Gil)");
        ImGui.SetNextItemWidth(ResolvedWidth());
        if (ImGuiEx.InputFancyNumeric("##RaffleTicketCost", ref cost, 100_000))
        {
            config.Raffle.TicketCost = Math.Max(0, cost);
            config.Save();
        }
    }

    private static void DrawMaxTickets(PluginConfiguration config)
    {
        var max = config.Raffle.MaxTicketsPerPlayer;
        ImGui.TextDisabled("Max Tickets Per Player");
        ImGui.SetNextItemWidth(ResolvedWidth() + ImGui.GetFrameHeight() * 2f + 2f);
        if (ImGui.InputInt("##RaffleMaxTickets", ref max, 1, 10))
        {
            config.Raffle.MaxTicketsPerPlayer = Math.Clamp(max, 1, RaffleGameIds.MaxTicketPoolSize);
            config.Save();
        }
    }

    private static void DrawBoostedPot(PluginConfiguration config)
    {
        var pot = (int)config.Raffle.BoostedPot;
        ImGui.TextDisabled("Boosted Pot (Gil)");
        ImGui.SetNextItemWidth(ResolvedWidth());
        if (ImGuiEx.InputFancyNumeric("##RaffleBoostedPot", ref pot, 100_000))
        {
            config.Raffle.BoostedPot = Math.Max(0, pot);
            config.Save();
        }
    }

    private static void DrawTradesToPotPercent(PluginConfiguration config)
    {
        ImGui.Spacing();
        var pct = config.Raffle.TradesToPotPercent;
        ImGui.TextDisabled("Trades to Pot (%)");
        ImGui.SetNextItemWidth(ResolvedWidth());
        if (ImGui.SliderInt("##RaffleTradesToPot", ref pct, 0, 100, "%d%%"))
        {
            config.Raffle.TradesToPotPercent = Math.Clamp(pct, 0, 100);
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
