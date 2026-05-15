using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Services;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>Renders the player waitlist sidebar, including the currently hosted player row, a scrollable queue table with per-row remind and remove buttons, and a manual player add field at the bottom.</summary>

namespace MiniGamesEmporium.UI.Components;
public sealed class QueuePanel
{
    private readonly SessionService sessionService;
    public QueuePanel(SessionService sessionService) => this.sessionService = sessionService;
    private const float QueueListMinScrollHeightPx = 96f;
    private static readonly Vector4 ReminderButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 ReminderButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 ReminderButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);
    private static readonly Vector4 ReminderSentButton        = new(0.28f, 0.28f, 0.28f, 1f);
    private static readonly Vector4 ReminderSentButtonHovered = new(0.38f, 0.38f, 0.38f, 1f);
    private static readonly Vector4 ReminderSentButtonActive  = new(0.48f, 0.48f, 0.48f, 1f);
    private static readonly Vector4 AnnounceButton        = new(0.55f, 0.50f, 0.04f, 1f);
    private static readonly Vector4 AnnounceButtonHovered = new(0.72f, 0.65f, 0.05f, 1f);
    private static readonly Vector4 AnnounceButtonActive  = new(0.90f, 0.82f, 0.06f, 1f);
    public void Draw(
        ref string manualAddBuffer,
        bool fillColumnHeight = false,
        string? currentSessionPlayerName = null,
        Func<string, bool>? hasBeenReminded = null,
        Action<string, int>? onManualReminder = null,
        Action? onAnnounceKeyword = null)
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonMagenta, "Player queue");
        if (onAnnounceKeyword != null)
        {
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button,        AnnounceButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, AnnounceButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  AnnounceButtonActive);
            if (ImGui.SmallButton("Announce Keyword##AnnounceKw"))
                onAnnounceKeyword.Invoke();
            ImGui.PopStyleColor(3);
        }
        ImGui.Separator();
        ImGui.Spacing();
        var currentName = string.IsNullOrWhiteSpace(currentSessionPlayerName)
            ? null
            : currentSessionPlayerName.Trim();
        DrawCurrentHostingRow(currentName, hasBeenReminded, onManualReminder);
        var style = ImGui.GetStyle();
        var footerReserve =
            style.ItemSpacing.Y * 2f
            + ImGui.GetFrameHeight()
            + style.ItemInnerSpacing.Y;
        var listScrollH = fillColumnHeight
            ? MathF.Max(QueueListMinScrollHeightPx, ImGui.GetContentRegionAvail().Y - footerReserve)
            : 180f;
        DrawQueueList(listScrollH, currentName, hasBeenReminded, onManualReminder);
        ImGui.Spacing();
        DrawManualAddRow(ref manualAddBuffer);
    }
    private void DrawCurrentHostingRow(string? currentSessionPlayerName, Func<string, bool>? hasBeenReminded, Action<string, int>? onManualReminder)
    {
        if (currentSessionPlayerName == null)
            return;
        ImGui.TextDisabled("Current");
        ImGui.TextUnformatted(currentSessionPlayerName);
        if (onManualReminder != null)
        {
            ImGui.SameLine();
            var reminded = hasBeenReminded?.Invoke(currentSessionPlayerName) ?? false;
            ImGui.PushStyleColor(ImGuiCol.Button,        reminded ? ReminderSentButton        : ReminderButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, reminded ? ReminderSentButtonHovered : ReminderButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  reminded ? ReminderSentButtonActive  : ReminderButtonActive);
            if (ImGui.SmallButton("Remind##RemindCurrent"))
                onManualReminder.Invoke(currentSessionPlayerName, 0);
            ImGui.PopStyleColor(3);
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
    private void DrawQueueList(
        float listScrollHeightPx,
        string? currentSessionPlayerName,
        Func<string, bool>? hasBeenReminded,
        Action<string, int>? onManualReminder)
    {
        var queue = this.sessionService.Queue;
        var hasVisibleWaiting = HasAnyWaitingRow(queue, currentSessionPlayerName);
        if (!hasVisibleWaiting)
        {
            using var emptyStretch = ImRaii.Child("##QueueEmptyPad", new Vector2(0, listScrollHeightPx), false, ImGuiWindowFlags.NoScrollbar);
            if (emptyStretch.Success)
                ImGui.TextDisabled("Waitlist is empty.");
            return;
        }
        var showReminder = onManualReminder != null;
        var actionsColWidth = showReminder ? 84f : 26f;
        int removeIndex = -1;
        int remindIndex = -1;
        int remindDisplayOrdinal = 0;
        using var table = ImRaii.Table(
            "##QueueTable_v6",
            2,
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(0, listScrollHeightPx));
        if (!table.Success) return;
        var waitColumnTitle = currentSessionPlayerName != null ? "Waiting" : "Player";
        ImGui.TableSetupColumn(waitColumnTitle, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##Actions", ImGuiTableColumnFlags.WidthFixed, actionsColWidth);
        ImGui.TableHeadersRow();
        // Start at 1 when there is a real current player so displayed ordinals match the tells.
        var displayOrdinal = currentSessionPlayerName != null ? 1 : 0;
        var firstWaiting = true;
        for (var i = 0; i < queue.Count; i++)
        {
            if (RowIsCurrentPlayer(queue[i], currentSessionPlayerName))
                continue;
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            displayOrdinal++;
            var label = firstWaiting ? $"[Next] {queue[i]}" : $"{displayOrdinal}. {queue[i]}";
            firstWaiting = false;
            ImGui.TextUnformatted(label);
            ImGui.TableSetColumnIndex(1);
            if (showReminder)
            {
                var reminded = hasBeenReminded?.Invoke(queue[i]) ?? false;
                ImGui.PushStyleColor(ImGuiCol.Button,        reminded ? ReminderSentButton        : ReminderButton);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, reminded ? ReminderSentButtonHovered : ReminderButtonHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  reminded ? ReminderSentButtonActive  : ReminderButtonActive);
                if (ImGui.SmallButton($"Remind##Remind{i}"))
                {
                    remindIndex = i;
                    remindDisplayOrdinal = displayOrdinal;
                }
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
            }
            if (ImGui.SmallButton($"x##Remove{i}"))
                removeIndex = i;
        }
        if (remindIndex >= 0)
            onManualReminder!.Invoke(queue[remindIndex], remindDisplayOrdinal);
        if (removeIndex >= 0)
            this.sessionService.RemoveFromQueue(removeIndex);
    }
    private static bool HasAnyWaitingRow(IReadOnlyList<string> queue, string? currentSessionPlayerName)
    {
        for (var i = 0; i < queue.Count; i++)
        {
            if (!RowIsCurrentPlayer(queue[i], currentSessionPlayerName))
                return true;
        }
        return false;
    }
    private static bool RowIsCurrentPlayer(string queuedName, string? currentSessionPlayerName)
    {
        if (currentSessionPlayerName == null)
            return false;
        var qAt = queuedName.IndexOf('@');
        var qName = qAt < 0 ? queuedName : queuedName[..qAt];
        var cAt = currentSessionPlayerName.IndexOf('@');
        var cName = cAt < 0 ? currentSessionPlayerName : currentSessionPlayerName[..cAt];
        return qName.Equals(cName, StringComparison.OrdinalIgnoreCase);
    }
    private void DrawManualAddRow(ref string buffer)
    {
        ImGui.SetNextItemWidth(-60f);
        ImGui.InputText("##ManualAdd", ref buffer, 64);
        ImGui.SameLine();
        if (ImGui.Button("Add##QueueAdd") && !string.IsNullOrWhiteSpace(buffer))
        {
            this.sessionService.TryEnqueuePlayer(buffer.Trim());
            buffer = string.Empty;
        }
    }
}
