using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Actions;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using System.Numerics;

/// <summary>Renders an inline stats panel at the bottom of the Deathroll Tournament game view, showing the total pot with a yell button, boosted pot, entry cost, player/round counts, and a donation input row.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollStatsTab
{
    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private readonly ChatQueueService chatQueue;
    private readonly HistoryService historyService;
    private int _donationInput = 0;
    private static readonly Vector4 YellButtonColour        = new(0.72f, 0.55f, 0.00f, 1f);
    private static readonly Vector4 YellButtonColourHovered = new(0.88f, 0.68f, 0.00f, 1f);
    private static readonly Vector4 YellButtonColourActive  = new(0.58f, 0.44f, 0.00f, 1f);

    public DeathrollStatsTab(PluginConfiguration config, DeathrollTournamentService deathrollService, ChatQueueService chatQueue, HistoryService historyService)
    {
        this.config           = config;
        this.deathrollService = deathrollService;
        this.chatQueue        = chatQueue;
        this.historyService   = historyService;
    }

    public static float GetInlineHeight()
    {
        var rowH   = ImGui.GetTextLineHeight() + ImGui.GetStyle().CellPadding.Y * 2f;
        var inputH = ImGui.GetFrameHeight()    + ImGui.GetStyle().CellPadding.Y * 2f;
        return 5 * rowH + inputH + ImGui.GetStyle().WindowPadding.Y * 2f + 4f;
    }

    public void DrawInline()
    {
        var totalPot      = this.deathrollService.ComputeTotalPot();
        var cfg           = this.config.DeathrollTournament;
        var activeSession = this.config.DeathrollSession;
        var tournament    = this.config.DeathrollTournamentSession;
        var entryCost  = tournament?.EntryCostAtStart  ?? activeSession?.EntryCost  ?? cfg.EntryCost;
        var boostedPot = tournament?.BoostedPotAtStart ?? activeSession?.BoostedPot ?? cfg.BoostedPot;
        using var child = ImRaii.Child("##DeathrollStatsPanel", new Vector2(-1, GetInlineHeight()), true);
        if (!child.Success) return;
        using var table = ImRaii.Table("##DeathrollStatsTable", 3, ImGuiTableFlags.None, new Vector2(-1, 0));
        if (!table.Success) return;
        ImGui.TableSetupColumn("##DSLabel",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##DSAction", ImGuiTableColumnFlags.WidthFixed, 130f);
        ImGui.TableSetupColumn("##DSValue",  ImGuiTableColumnFlags.WidthFixed, 180f);
        DrawPotRow(totalPot);
        DrawRow("Entry Cost",  $"{entryCost:N0} Gil",              EmporiumNeonTheme.DeathrollTournamentPink);
        DrawRow("Boosted Pot", $"{boostedPot:N0} Gil",             EmporiumNeonTheme.WinGold);
        var players = tournament?.PlayerCountAtStart ?? cfg.PaidPlayers.Count;
        DrawRow("Players",     players.ToString(),                  EmporiumNeonTheme.NeonCyan);
        var roundLabel = tournament != null
            ? $"Round {tournament.CurrentRoundIndex + 1} of {tournament.Rounds.Count}"
            : "Not started";
        DrawRow("Round",       roundLabel,                          EmporiumNeonTheme.NeonMagenta);
        DrawDonationRow();
    }

    private void DrawPotRow(long totalPot)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled("Total Pot");
        ImGui.TableSetColumnIndex(1);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4f, 0f));
        ImGui.PushStyleColor(ImGuiCol.Button,        YellButtonColour);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellButtonColourHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellButtonColourActive);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Pot", "##DRYellPot");
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar();
        if (clicked) AnnouncePot.Execute(this.config, this.chatQueue, totalPot);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{totalPot:N0} Gil");
    }

    private void DrawDonationRow()
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled("Adjust Pot (Gil)");
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputInt("##DRDonation", ref this._donationInput, 0, 0);
        ImGui.TableSetColumnIndex(2);
        {
            using var green = UIHelper.PushGreenButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Add", "##DRAddDonation") && this._donationInput > 0)
            {
                AddDonation.Execute(this.config, this.historyService, this._donationInput);
                this._donationInput = 0;
            }
        }
        ImGui.SameLine();
        {
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Minus, "Remove", "##DRRemoveDonation") && this._donationInput > 0)
            {
                RemoveDonation.Execute(this.config, this.historyService, this._donationInput);
                this._donationInput = 0;
            }
        }
    }

    private static void DrawRow(string label, string value, Vector4 colour)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(colour, value);
    }
}
