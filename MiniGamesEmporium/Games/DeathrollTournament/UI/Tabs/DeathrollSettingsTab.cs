using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.DeathrollTournament.UI.Components;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Draws the Settings tab for Deathroll Tournament.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollSettingsTab
{
    private readonly PluginConfiguration config;

    public DeathrollSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        using var scroll = ImRaii.Child("##DeathrollSettingsScroll");
        if (!scroll) return;
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Deathroll Tournament");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        DrawEntryCost();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DeathrollPrizeFields.Draw(this.config, "Settings", -1f);
        if (this.config.DeathrollTournament.PrizeType == DeathrollPrizeType.Gil)
        {
            ImGui.Spacing();
            DrawBoostedPot();
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawAutoJoinSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawBettingSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawAutoNextMatchSection();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawBestOfSection();
    }

    private void DrawEntryCost()
    {
        var cost = (int)this.config.DeathrollTournament.EntryCost;
        ImGui.TextDisabled("Entry Cost (Gil)");
        ImGui.SetNextItemWidth(FancyWidth());
        if (ImGuiEx.InputFancyNumeric("##DREntryCostSettings", ref cost, 100_000))
        {
            this.config.DeathrollTournament.EntryCost = Math.Max(0, cost);
            this.config.Save();
        }
    }

    private void DrawBoostedPot()
    {
        var pot = (int)this.config.DeathrollTournament.BoostedPot;
        ImGui.TextDisabled("Boosted Pot (Gil)");
        ImGui.SetNextItemWidth(FancyWidth());
        if (ImGuiEx.InputFancyNumeric("##DRBoostedPotSettings", ref pot, 100_000))
        {
            this.config.DeathrollTournament.BoostedPot = Math.Max(0, pot);
            this.config.Save();
        }
    }

    private static float FancyWidth() =>
        MathF.Max(80f, ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 2f - 2f);

    private void DrawAutoJoinSection()
        => DeathrollAutoJoinFields.Draw(this.config, "Settings", -1f);

    private void DrawBettingSection()
        => DeathrollBettingFields.Draw(this.config, "Settings", -1f);

    private void DrawAutoNextMatchSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Match Progression");
        ImGui.Spacing();
        var autoNext = this.config.DeathrollTournament.AutoNextMatch;
        if (ImGui.Checkbox("Auto Next Match##DRAutoNextMatch", ref autoNext))
        {
            this.config.DeathrollTournament.AutoNextMatch = autoNext;
            this.config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("- automatically advances to the next match when a match completes");
        if (autoNext)
        {
            ImGui.Spacing();
            ImGui.Indent();
            var delay = this.config.DeathrollTournament.AutoNextMatchDelaySeconds;
            ImGui.TextDisabled("Delay (seconds)");
            ImGui.SetNextItemWidth(200f);
            if (ImGui.InputInt("##DRAutoNextDelay", ref delay, 1, 5))
            {
                this.config.DeathrollTournament.AutoNextMatchDelaySeconds = Math.Clamp(delay, 0, 60);
                this.config.Save();
            }
            ImGui.Unindent();
        }
        ImGui.Spacing();
        var autoCatch = this.config.DeathrollTournament.AutoCatchNextRound;
        if (ImGui.Checkbox("Auto Catch Next Round##DRAutoCatch", ref autoCatch))
        {
            this.config.DeathrollTournament.AutoCatchNextRound = autoCatch;
            this.config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("- starts the next match when an upcoming player rolls /random 10");
    }

    private void DrawBestOfSection()
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Best-of per Round (defaults)");
        ImGui.Spacing();
        ImGui.TextDisabled("These are the default values applied when starting a new tournament.");
        ImGui.TextDisabled("Adjust them in the game panel before clicking Start Tournament.");
        ImGui.Spacing();
        var list = this.config.DeathrollTournament.BestOfPerRound;
        for (var i = 0; i < Math.Min(list.Count, 5); i++)
        {
            var val = list[i];
            ImGui.SetNextItemWidth(120f);
            if (ImGui.InputInt($"Round {i + 1}##DRBOSettings{i}", ref val, 2, 2))
            {
                list[i] = Math.Max(1, val % 2 == 0 ? val + 1 : val);
                this.config.Save();
            }
            ImGui.TextDisabled($"  Best of {list[i]} (need {list[i] / 2 + 1} win(s))");
        }
    }
}
