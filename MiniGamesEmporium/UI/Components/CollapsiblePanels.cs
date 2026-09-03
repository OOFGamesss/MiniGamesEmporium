using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

/// <summary>Helpers that add a collapse tag to a game's side pane and bottom stats strip.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class CollapsiblePanels
{
    public static bool IsCollapsed(string panelKey) =>
        UiLayoutState.IsCollapsed(panelKey);

    public static void SetupSideColumns(string panelKey, string idPrefix, float paneWidth)
    {
        ImGui.TableSetupColumn($"{idPrefix}Tag", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, PanelEdgeTag.Size());
        if (!IsCollapsed(panelKey))
            ImGui.TableSetupColumn($"{idPrefix}Side", ImGuiTableColumnFlags.WidthFixed, paneWidth);
    }

    public static int SideColumnCount(string panelKey) =>
        IsCollapsed(panelKey) ? 2 : 3;

    public static bool DrawSideTag(string panelKey, string id, Vector4 accent, string panelName)
    {
        var height    = ImGui.GetContentRegionAvail().Y;
        var collapsed = IsCollapsed(panelKey);

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().CellPadding.X);

        if (PanelEdgeTag.DrawVertical(id, height, collapsed, accent, panelName))
        {
            UiLayoutState.Toggle(panelKey);
            collapsed = !collapsed;
        }
        return !collapsed;
    }

    public static float StatsReserveHeight(string panelKey, float panelHeight) =>
        PanelEdgeTag.Size() + ImGui.GetStyle().ItemSpacing.Y
        + (IsCollapsed(panelKey) ? 0f : panelHeight);

    public static void DrawStatsStrip(string panelKey, string id, Vector4 accent, string panelName, Action drawPanel)
    {
        var collapsed = IsCollapsed(panelKey);
        if (PanelEdgeTag.DrawHorizontal(id, -1f, collapsed, accent, panelName))
        {
            UiLayoutState.Toggle(panelKey);
            collapsed = !collapsed;
        }
        if (!collapsed) drawPanel();
    }
}
