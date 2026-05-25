using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
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
    private string comboFilter = string.Empty;
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
    private static readonly Vector4 PauseButton        = new(0.62f, 0.35f, 0.00f, 1f);
    private static readonly Vector4 PauseButtonHovered = new(0.78f, 0.45f, 0.02f, 1f);
    private static readonly Vector4 PauseButtonActive  = new(0.92f, 0.56f, 0.04f, 1f);
    private static readonly Vector4 ContinueButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 ContinueButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 ContinueButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);
    public void Draw(
        IReadOnlyList<string> nearbyPlayers,
        bool fillColumnHeight = false,
        string? currentSessionPlayerName = null,
        Func<string, bool>? hasBeenReminded = null,
        Action<string, int>? onManualReminder = null,
        Action? onAnnounceKeyword = null,
        Action? onToBackQueue = null,
        Action? onRemoveFromQueue = null,
        bool isQueuePaused = false,
        Action? onToggleQueuePause = null)
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonMagenta, "Player queue");
        if (onAnnounceKeyword != null)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        AnnounceButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, AnnounceButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  AnnounceButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Keyword", "##AnnounceKw"))
                onAnnounceKeyword.Invoke();
            ImGui.PopStyleColor(3);
        }
        if (onToggleQueuePause != null)
        {
            if (isQueuePaused)
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        ContinueButton);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ContinueButtonHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  ContinueButtonActive);
                if (UIHelper.IconTextButton(FontAwesomeIcon.Play, "Continue Queue", "##ToggleQueuePause"))
                    onToggleQueuePause.Invoke();
                ImGui.PopStyleColor(3);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        PauseButton);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PauseButtonHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  PauseButtonActive);
                if (UIHelper.IconTextButton(FontAwesomeIcon.Pause, "Pause Queue", "##ToggleQueuePause"))
                    onToggleQueuePause.Invoke();
                ImGui.PopStyleColor(3);
            }
        }
        ImGui.Separator();
        ImGui.Spacing();
        var currentName = string.IsNullOrWhiteSpace(currentSessionPlayerName)
            ? null
            : currentSessionPlayerName.Trim();
        DrawCurrentHostingRow(currentName, hasBeenReminded, onManualReminder, onToBackQueue, onRemoveFromQueue);
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
        DrawManualAddRow(nearbyPlayers);
    }
    private void DrawCurrentHostingRow(
        string? currentSessionPlayerName,
        Func<string, bool>? hasBeenReminded,
        Action<string, int>? onManualReminder,
        Action? onToBackQueue,
        Action? onRemoveFromQueue)
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
            if (UIHelper.IconTextButton(FontAwesomeIcon.Bell, "Remind", "##RemindCurrent"))
                onManualReminder.Invoke(currentSessionPlayerName, 0);
            ImGui.PopStyleColor(3);
        }
        if (onToBackQueue != null)
        {
            using (UIHelper.PushOrangeButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Redo, "To Back Q", "##QueueToBack"))
                    onToBackQueue();
            }
            ImGui.SameLine();
        }
        if (onRemoveFromQueue != null)
        {
            using (UIHelper.PushRedButtonColours())
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.UserMinus, "Remove from Q", "##QueueRemove"))
                    onRemoveFromQueue();
            }
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
        var actionsColWidth = showReminder ? 84f : 30f;
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
                if (UIHelper.IconTextButton(FontAwesomeIcon.Bell, "Remind", $"##Remind{i}"))
                {
                    remindIndex = i;
                    remindDisplayOrdinal = displayOrdinal;
                }
                ImGui.PopStyleColor(3);
                ImGui.SameLine();
            }
            if (UIHelper.IconTextButton(FontAwesomeIcon.Times, string.Empty, $"##Remove{i}"))
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
    private void DrawManualAddRow(IReadOnlyList<string> nearbyPlayers)
    {
        var queue = this.sessionService.Queue;
        var preview = nearbyPlayers.Count == 0 ? "No players nearby" : "Add player to queue...";
        ImGui.SetNextItemWidth(-1);
        using var combo = ImRaii.Combo("##NearbyAdd", preview, ImGuiComboFlags.HeightLarge);
        if (!combo.Success) return;
        if (nearbyPlayers.Count == 0)
        {
            ImGui.TextDisabled("No players in the area.");
            return;
        }
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##NearbyFilter", ref this.comboFilter, 64);
        var any = false;
        foreach (var player in nearbyPlayers)
        {
            if (IsAlreadyQueued(player, queue)) continue;
            if (!player.Contains(this.comboFilter, StringComparison.OrdinalIgnoreCase)) continue;
            any = true;
            if (!ImGui.Selectable(player)) continue;
            this.sessionService.TryEnqueuePlayer(player);
            this.comboFilter = string.Empty;
            ImGui.CloseCurrentPopup();
        }
        if (!any) ImGui.TextDisabled("No matches.");
    }
    private static bool IsAlreadyQueued(string nearbyEntry, IReadOnlyList<string> queue)
    {
        var at = nearbyEntry.IndexOf('@');
        var nearbyName = at < 0 ? nearbyEntry : nearbyEntry[..at];
        foreach (var q in queue)
        {
            var qAt = q.IndexOf('@');
            var qName = qAt < 0 ? q : q[..qAt];
            if (qName.Equals(nearbyName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}
