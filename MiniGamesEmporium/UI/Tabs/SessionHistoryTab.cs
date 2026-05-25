using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.State;
using MiniGamesEmporium.UI.Components;
using System.Numerics;

/// <summary>Draws the Session History tab, listing completed sessions as collapsible entries with pot breakdown, winner, players played, and timestamp, with options to delete individual sessions or clear all history.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class SessionHistoryTab
{
    private readonly PluginConfiguration config;
    private int? pendingDeleteIndex;

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
        var clearClicked = false;
        using (UIHelper.PushRedButtonColours())
            clearClicked = UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Clear History", "##ClearSessionHistory");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Permanently delete all session history. This cannot be undone.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        var deleteRequested = false;
        using (var child = ImRaii.Child("##SessionHistoryScroll", new Vector2(-1, -1), false))
        {
            if (child.Success)
            {
                for (var i = history.Count - 1; i >= 0; i--)
                {
                    if (i == history.Count - 1)
                        ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                    if (DrawSessionEntry(history[i], i))
                    {
                        this.pendingDeleteIndex = i;
                        deleteRequested = true;
                    }
                }
            }
        }
        if (clearClicked)
            ImGui.OpenPopup("##ConfirmClearAll");
        if (deleteRequested)
            ImGui.OpenPopup("##ConfirmDeleteSession");
        DrawClearAllConfirmModal();
        DrawDeleteConfirmModal();
    }

    private bool DrawSessionEntry(SessionRecord record, int index)
    {
        var localTime    = record.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        var winLabel     = string.IsNullOrEmpty(record.Winner) ? "No Win" : $"Won by {record.Winner}";
        var header       = $"{record.GameName}  |  {winLabel}  |  {localTime}##SessionEntry{index}";
        var headerColour = string.IsNullOrEmpty(record.Winner)
            ? EmporiumNeonTheme.NeonCyan
            : EmporiumNeonTheme.WinGold;
        ImGui.PushStyleColor(ImGuiCol.Text, headerColour);
        var open = ImGui.CollapsingHeader(header);
        ImGui.PopStyleColor();
        if (!open) return false;
        ImGui.Indent(16f);
        DrawDetailRow("Game",            record.GameName);
        DrawDetailRow("Winner",          string.IsNullOrEmpty(record.Winner) ? "-" : record.Winner);
        DrawDetailRow("Total Pot",       $"{record.TotalPot:N0} gil");
        DrawDetailRow("Boosted Pot",     $"{record.BoostedPot:N0} gil");
        DrawDetailRow("Taken in Trades", $"{record.AmountInTrades:N0} gil");
        DrawDetailRow("Players Played",  record.PlayersPlayed.ToString());
        if (record.RoundsPlayed.HasValue)
            DrawDetailRow("Rounds Played",  record.RoundsPlayed.Value.ToString());
        if (record.MatchesPlayed.HasValue)
            DrawDetailRow("Matches Played", record.MatchesPlayed.Value.ToString());
        DrawDetailRow("Timestamp",       record.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        ImGui.Spacing();
        bool deleteClicked;
        using (UIHelper.PushRedButtonColours())
            deleteClicked = UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Delete Session", "##DeleteSession");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Delete this session. This cannot be undone.");
        ImGui.Unindent(16f);
        ImGui.Spacing();
        return deleteClicked;
    }

    private void DrawDeleteConfirmModal()
    {
        var open = true;
        using var modal = ImRaii.PopupModal("##ConfirmDeleteSession", ref open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!modal.Success) return;
        ImGui.TextUnformatted("Delete this session? This cannot be undone.");
        ImGui.Spacing();
        using (UIHelper.PushRedButtonColours())
        {
            if (ImGui.Button("Delete", new Vector2(100, 0)) && this.pendingDeleteIndex.HasValue)
            {
                this.config.SessionHistory.RemoveAt(this.pendingDeleteIndex.Value);
                this.config.Save();
                this.pendingDeleteIndex = null;
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(100, 0)))
        {
            this.pendingDeleteIndex = null;
            ImGui.CloseCurrentPopup();
        }
    }

    private void DrawClearAllConfirmModal()
    {
        var open = true;
        using var modal = ImRaii.PopupModal("##ConfirmClearAll", ref open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!modal.Success) return;
        ImGui.TextUnformatted("Clear all session history? This cannot be undone.");
        ImGui.Spacing();
        using (UIHelper.PushRedButtonColours())
        {
            if (ImGui.Button("Clear All", new Vector2(100, 0)))
            {
                this.config.SessionHistory.Clear();
                this.config.Save();
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(100, 0)))
            ImGui.CloseCurrentPopup();
    }

    private static void DrawDetailRow(string label, string value)
    {
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"{label}:");
        ImGui.SameLine(160f);
        ImGui.TextUnformatted(value);
    }
}
