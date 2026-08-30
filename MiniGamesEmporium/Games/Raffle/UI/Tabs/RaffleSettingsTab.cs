using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.UI.Components;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Draws the Settings tab for the Raffle game defaults.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Tabs;
public sealed class RaffleSettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.RaffleTeal;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;

    public RaffleSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        using var scroll = ImRaii.Child("##RaffleSettingsScroll");
        if (!scroll) return;
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Raffle");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        if (this.config.RaffleSession != null)
        {
            SessionLockNotice.Draw(
                "A session is active. Tickets, pot, shuffle mode and closing time are locked until you stop the session.");
        }
        else
        {
            this.card.Draw("##RaffleTicketCard", "Tickets and Pot", CardAccent, CardTitle, DrawTicketAndPotBody);
            this.card.Draw("##RaffleShuffleCard", "Shuffle Mode", CardAccent, CardTitle,
                () => RaffleShuffleModeFields.Draw(this.config, "Settings"));
            this.card.Draw("##RaffleCloseCard", "Closing Time", CardAccent, CardTitle,
                () => RaffleCloseTimeFields.DrawConfig(this.config, "Settings", -1f));
        }
        this.card.Draw("##RaffleJoinCard", "Auto Join", CardAccent, CardTitle,
            () => RaffleAutoJoinFields.Draw(this.config, "Settings", -1f));
    }

    private void DrawTicketAndPotBody()
    {
        DrawTicketCost();
        DrawBoostedPot();
        DrawMaxTickets();
        DrawTradesToPotPercent();
    }

    private void DrawTicketCost()
    {
        var cost = (int)this.config.Raffle.TicketCost;
        ImGui.TextDisabled("Ticket Cost (Gil)");
        ImGui.SetNextItemWidth(FancyWidth());
        if (ImGuiEx.InputFancyNumeric("##RaffleTicketCostSettings", ref cost, 100_000))
        {
            this.config.Raffle.TicketCost = Math.Max(0, cost);
            this.config.Save();
        }
    }

    private void DrawMaxTickets()
    {
        var max = this.config.Raffle.MaxTicketsPerPlayer;
        ImGui.TextDisabled("Max Tickets Per Player");
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputInt("##RaffleMaxTicketsSettings", ref max, 1, 10))
        {
            this.config.Raffle.MaxTicketsPerPlayer = Math.Clamp(max, 1, RaffleGameIds.MaxTicketPoolSize);
            this.config.Save();
        }
    }

    private void DrawBoostedPot()
    {
        var pot = (int)this.config.Raffle.BoostedPot;
        ImGui.TextDisabled("Boosted Pot (Gil)");
        ImGui.SetNextItemWidth(FancyWidth());
        if (ImGuiEx.InputFancyNumeric("##RaffleBoostedPotSettings", ref pot, 100_000))
        {
            this.config.Raffle.BoostedPot = Math.Max(0, pot);
            this.config.Save();
        }
    }

    private void DrawTradesToPotPercent()
    {
        var pct = this.config.Raffle.TradesToPotPercent;
        ImGui.TextDisabled("Trades to Pot (%)");
        ImGui.SetNextItemWidth(FancyWidth());
        if (ImGui.SliderInt("##RaffleTradesToPotSettings", ref pct, 0, 100, "%d%%"))
        {
            this.config.Raffle.TradesToPotPercent = Math.Clamp(pct, 0, 100);
            this.config.Save();
        }
    }

    private static float FancyWidth() =>
        MathF.Max(80f, ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 2f - 2f);
}
