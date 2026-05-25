using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Actions;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using System.Linq;
using System.Numerics;

/// <summary>Renders an inline stats panel at the bottom of the game view, showing the total pot with a yell button, boosted pot, Gil taken in trades, players played, optionally the queue length, and a donation input row.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Tabs;
public sealed class Bar777StatsTab
{
    private readonly PluginConfiguration config;
    private readonly ChatQueueService chatQueue;
    private int _donationInput = 0;
    private static readonly Vector4 YellButtonColour        = new(0.72f, 0.55f, 0.00f, 1f);
    private static readonly Vector4 YellButtonColourHovered = new(0.88f, 0.68f, 0.00f, 1f);
    private static readonly Vector4 YellButtonColourActive  = new(0.58f, 0.44f, 0.00f, 1f);
    public Bar777StatsTab(PluginConfiguration config, ChatQueueService chatQueue)
    {
        this.config = config;
        this.chatQueue = chatQueue;
    }
    public static float GetInlineHeight(bool showQueue)
    {
        var rowH   = ImGui.GetTextLineHeight() + ImGui.GetStyle().CellPadding.Y * 2f;
        var inputH = ImGui.GetFrameHeight()    + ImGui.GetStyle().CellPadding.Y * 2f;
        var rows   = showQueue ? 5 : 4;
        return rows * rowH + inputH + ImGui.GetStyle().WindowPadding.Y * 2f + 4f;
    }
    public void DrawInline(bool showQueue)
    {
        var totalTraded = this.config.Bar777.SessionTradedTotal;
        var totalPot    = this.config.Bar777.BoostedPot + totalTraded;
        using var child = ImRaii.Child("##Bar777StatsPanel", new Vector2(-1, GetInlineHeight(showQueue)), true);
        if (!child.Success) return;
        using var table = ImRaii.Table("##Bar777StatsTable", 3, ImGuiTableFlags.None, new Vector2(-1, 0));
        if (!table.Success) return;
        ImGui.TableSetupColumn("##StatsLabel",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##StatsAction", ImGuiTableColumnFlags.WidthFixed, 130f);
        ImGui.TableSetupColumn("##StatsValue",  ImGuiTableColumnFlags.WidthFixed, 180f);
        DrawTotalPotRow(totalPot);
        DrawRow("Boosted Pot",     $"{this.config.Bar777.BoostedPot:N0} Gil", EmporiumNeonTheme.WinGold);
        DrawRow("Taken in Trades", $"{totalTraded:N0} Gil",                   EmporiumNeonTheme.NeonCyan);
        DrawRow("Players Played",  this.config.Bar777.PlayersPlayed.ToString(), EmporiumNeonTheme.NeonMagenta);
        if (showQueue)
            DrawRow("In Queue", this.config.QueuedPlayers.Count.ToString(), new Vector4(0.94f, 0.92f, 0.98f, 1f));
        DrawDonationRow();
    }
    private void DrawTotalPotRow(long totalPot)
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
        var yellClicked = UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Pot", "##YellPot");
        ImGui.PopStyleColor(3);
        if (yellClicked)
            AnnouncePot.Execute(this.config, this.chatQueue, totalPot);
        ImGui.PopStyleVar();
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
        ImGui.InputInt("##Bar777Donation", ref this._donationInput, 0, 0);
        ImGui.TableSetColumnIndex(2);
        {
            using var green = UIHelper.PushGreenButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Add", "##Bar777AddDonation") && this._donationInput > 0)
            {
                AddDonation.Execute(this.config, this._donationInput);
                this._donationInput = 0;
            }
        }
        ImGui.SameLine();
        {
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Minus, "Remove", "##Bar777RemoveDonation") && this._donationInput > 0)
            {
                RemoveDonation.Execute(this.config, this._donationInput);
                this._donationInput = 0;
            }
        }
    }
    private static void DrawRow(string label, string value, Vector4 valueColour)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(valueColour, value);
    }
}
