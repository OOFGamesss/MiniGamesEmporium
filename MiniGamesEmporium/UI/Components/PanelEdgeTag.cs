using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

/// <summary>Thin clickable strip drawn on a panel edge that collapses or expands the panel beside it.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class PanelEdgeTag
{
    private const float Thickness = 11f;
    private const float Rounding  = 3f;
    private const float MinSpan   = 24f;

    public static float Size() => Thickness * ImGuiHelpers.GlobalScale;

    public static bool DrawVertical(string id, float height, bool collapsed, Vector4 accent, string panelName, bool panelOnRight = true)
    {
        var icon = panelOnRight
            ? collapsed ? FontAwesomeIcon.AngleLeft : FontAwesomeIcon.AngleRight
            : collapsed ? FontAwesomeIcon.AngleRight : FontAwesomeIcon.AngleLeft;
        return Draw(id, new Vector2(Size(), MathF.Max(MinSpan, height)), collapsed, accent, icon, panelName);
    }

    public static bool DrawHorizontal(string id, float width, bool collapsed, Vector4 accent, string panelName)
    {
        var icon = collapsed ? FontAwesomeIcon.AngleUp : FontAwesomeIcon.AngleDown;
        var span = width > 0f ? width : ImGui.GetContentRegionAvail().X;
        return Draw(id, new Vector2(MathF.Max(MinSpan, span), Size()), collapsed, accent, icon, panelName);
    }

    private static bool Draw(string id, Vector2 size, bool collapsed, Vector4 accent, FontAwesomeIcon icon, string panelName)
    {
        var clicked = ImGui.InvisibleButton(id, size);
        var hovered = ImGui.IsItemHovered();
        var active  = ImGui.IsItemActive();

        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var dl  = ImGui.GetWindowDrawList();

        var rounding = Rounding * ImGuiHelpers.GlobalScale;
        var fill = accent with { W = active ? 0.85f : hovered ? 0.60f : collapsed ? 0.34f : 0.20f };
        dl.AddRectFilled(min, max, ImGui.GetColorU32(fill), rounding);
        var border = accent with { W = hovered || active ? 0.95f : 0.55f };
        dl.AddRect(min, max, ImGui.GetColorU32(border), rounding);

        DrawChevron(dl, min, max, icon, hovered || active);

        if (hovered)
            ImGui.SetTooltip(collapsed ? $"Show {panelName}" : $"Hide {panelName}");

        return clicked;
    }

    private static void DrawChevron(ImDrawListPtr dl, Vector2 min, Vector2 max, FontAwesomeIcon icon, bool emphasised)
    {
        var iconFont = UiBuilder.IconFont;
        var iconStr  = icon.ToIconString();

        var fontSize = ImGui.GetFontSize() * 0.80f;
        ImGui.PushFont(iconFont);
        var glyph = ImGui.CalcTextSize(iconStr) * (fontSize / ImGui.GetFontSize());
        ImGui.PopFont();

        var clipMin = dl.GetClipRectMin();
        var clipMax = dl.GetClipRectMax();
        var top     = MathF.Max(min.Y, clipMin.Y);
        var bottom  = MathF.Min(max.Y, clipMax.Y);
        if (bottom <= top) { top = min.Y; bottom = max.Y; }

        var centre = new Vector2((min.X + max.X) * 0.5f, (top + bottom) * 0.5f);
        var pos    = new Vector2(centre.X - glyph.X * 0.5f, centre.Y - glyph.Y * 0.5f);
        var colour = new Vector4(1f, 1f, 1f, emphasised ? 1f : 0.75f);
        dl.AddText(iconFont, fontSize, pos, ImGui.GetColorU32(colour), iconStr);
    }
}
