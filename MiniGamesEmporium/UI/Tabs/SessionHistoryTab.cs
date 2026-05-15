using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.State;
using MiniGamesEmporium.UI.Components;
using System.Numerics;

/// <summary>Draws the Session History tab, listing completed sessions as collapsible entries with pot breakdown, winner, players played, and timestamp, with an option to clear all history.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class SessionHistoryTab
{
    private readonly PluginConfiguration config;
    public SessionHistoryTab(PluginConfiguration config)
    {
        this.config = config;
    }
    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Session History");
        ImGui.Spacing();
        var history = this.config.SessionHistory;
        if (history.Count == 0)
        {
            ImGui.TextDisabled("No sessions recorded yet. Sessions are saved when you stop or end a session.");
            return;
        }
        ImGui.TextDisabled($"{history.Count} session(s) recorded.");
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.55f, 0.05f, 0.05f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.72f, 0.08f, 0.08f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.40f, 0.02f, 0.02f, 1f));
        if (ImGui.Button("Clear History##ClearSessionHistory"))
        {
            this.config.SessionHistory.Clear();
            this.config.Save();
        }
        ImGui.PopStyleColor(3);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        using var child = ImRaii.Child("##SessionHistoryScroll", new Vector2(-1, -1), false);
        if (!child.Success) return;
        for (var i = history.Count - 1; i >= 0; i--)
        {
            if (i == history.Count - 1)
                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
            DrawSessionEntry(history[i], i);
        }
    }
    private static void DrawSessionEntry(SessionRecord record, int index)
    {
        var localTime = record.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        var winLabel  = string.IsNullOrEmpty(record.Winner) ? "No Win" : $"Won by {record.Winner}";
        var header    = $"{record.GameName}  |  {winLabel}  |  {localTime}##SessionEntry{index}";
        var headerColour = string.IsNullOrEmpty(record.Winner)
            ? EmporiumNeonTheme.NeonCyan
            : EmporiumNeonTheme.WinGold;
        ImGui.PushStyleColor(ImGuiCol.Text, headerColour);
        var open = ImGui.CollapsingHeader(header);
        ImGui.PopStyleColor();
        if (!open) return;
        ImGui.Indent(16f);
        DrawDetailRow("Game",            record.GameName);
        DrawDetailRow("Winner",          string.IsNullOrEmpty(record.Winner) ? "-" : record.Winner);
        DrawDetailRow("Total Pot",       $"{record.TotalPot:N0} gil");
        DrawDetailRow("Boosted Pot",     $"{record.BoostedPot:N0} gil");
        DrawDetailRow("Taken in Trades", $"{record.AmountInTrades:N0} gil");
        DrawDetailRow("Players Played",  record.PlayersPlayed.ToString());
        DrawDetailRow("Timestamp",       record.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        ImGui.Unindent(16f);
        ImGui.Spacing();
    }
    private static void DrawDetailRow(string label, string value)
    {
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"{label}:");
        ImGui.SameLine(160f);
        ImGui.TextUnformatted(value);
    }
}
