using System.Numerics;
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
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.DeathrollTournamentPink;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;

    public DeathrollSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        using var scroll = ImRaii.Child("##DeathrollSettingsScroll");
        if (!scroll) return;
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Deathroll Tournament");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        var locked = this.config.DeathrollSession != null;
        if (locked)
            SessionLockNotice.Draw(
                "A session is active. Entry cost, prize, betting and best-of defaults are locked until you stop the session. Adjust best-of for the next tournament on the Bracket tab.");
        else
        {
            this.card.Draw("##DRSetEntryCard", "Entry Cost", CardAccent, CardTitle, DrawEntryCost);
            this.card.Draw("##DRSetPrizeCard", "Prize", CardAccent, CardTitle, DrawPrizeBody);
        }
        this.card.Draw("##DRSetJoinCard", "Auto Join", CardAccent, CardTitle, DrawAutoJoinSection);
        if (!locked)
            this.card.Draw("##DRSetBetCard", "Betting", CardAccent, CardTitle, DrawBettingSection);
        this.card.Draw("##DRSetNextCard", "Match Progression", CardAccent, CardTitle, DrawAutoNextMatchSection);
        if (!locked)
            this.card.Draw("##DRSetBestOfCard", "Best-of per Round (defaults)", CardAccent, CardTitle, DrawBestOfSection);
    }

    private void DrawPrizeBody()
    {
        DeathrollPrizeFields.Draw(this.config, "Settings", -1f);
        if (this.config.DeathrollTournament.PrizeType != DeathrollPrizeType.Gil) return;
        ImGui.Spacing();
        DrawBoostedPot();
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
