using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

/// <summary>Draws a single selectable navigation row for the main window sidebar.</summary>

namespace MiniGamesEmporium.UI.Components;
internal static class SidebarRow
{
    private const float IconBoxWidth = 26f;
    private const float PaddingX = 8f;
    private const float ExtraHeight = 6f;
    private const float PillPaddingX = 6f;
    private const float PillPaddingY = 2f;
    private const float PillDotRadius = 3f;
    private const float PillDotGap = 4f;
    private const float PillGap = 6f;

    private static readonly Vector4 PillForeground = new(1f, 1f, 1f, 1f);

    internal static float CalcPillWidth(string text)
    {
        var scale = ImGuiHelpers.GlobalScale;
        return PillPaddingX * 2f * scale
            + PillDotRadius * 2f * scale
            + PillDotGap * scale
            + ImGui.CalcTextSize(text).X;
    }

    internal static float CalcRowWidth(string label, float indent, bool hasTrailingIcon, string? pillText = null)
    {
        var style = ImGui.GetStyle();
        var scale = ImGuiHelpers.GlobalScale;

        var width = PaddingX * scale + indent
            + IconBoxWidth * scale
            + style.ItemInnerSpacing.X
            + ImGui.CalcTextSize(label).X
            + PaddingX * scale;

        if (pillText != null)
            width += CalcPillWidth(pillText) + PillGap * scale;

        if (!hasTrailingIcon)
            return width;

        ImGui.PushFont(UiBuilder.IconFont);
        var chevronWidth = ImGui.CalcTextSize(FontAwesomeIcon.ChevronRight.ToIconString()).X;
        ImGui.PopFont();

        return width + chevronWidth + PillGap * scale;
    }

    internal static bool Draw(
        string id,
        FontAwesomeIcon icon,
        string label,
        bool selected,
        float indent = 0f,
        FontAwesomeIcon? trailingIcon = null,
        Vector4? accentColour = null,
        string? trailingPill = null,
        Vector4? trailingPillColour = null)
    {
        var style = ImGui.GetStyle();
        var scale = ImGuiHelpers.GlobalScale;

        var width = ImGui.GetContentRegionAvail().X;
        var height = ImGui.GetTextLineHeight() + style.FramePadding.Y * 2f + ExtraHeight * scale;
        var origin = ImGui.GetCursorScreenPos();

        var clicked = ImGui.Selectable($"##sidebar_{id}", selected, ImGuiSelectableFlags.None, new Vector2(width, height));

        var accent = accentColour ?? EmporiumNeonTheme.NeonCyan;
        var colour = selected || ImGui.IsItemHovered()
            ? ImGui.ColorConvertFloat4ToU32(accent)
            : ImGui.GetColorU32(ImGuiCol.Text);

        var dl = ImGui.GetWindowDrawList();
        var padX = PaddingX * scale + indent;
        var iconBox = IconBoxWidth * scale;

        DrawIcon(dl, icon, origin, padX, iconBox, height, colour);
        DrawLabel(dl, label, origin, padX + iconBox + style.ItemInnerSpacing.X, height, colour);

        var rightX = origin.X + width - PaddingX * scale;

        if (trailingIcon.HasValue)
            rightX = DrawTrailingIcon(dl, trailingIcon.Value, rightX, origin.Y, height, colour) - PillGap * scale;

        if (trailingPill != null)
            DrawTrailingPill(dl, trailingPill, trailingPillColour ?? EmporiumNeonTheme.SuccessMint, rightX, origin.Y, height, scale);

        return clicked;
    }

    private static void DrawIcon(ImDrawListPtr dl, FontAwesomeIcon icon, Vector2 origin, float padX, float iconBox, float height, uint colour)
    {
        var iconStr = icon.ToIconString();
        ImGui.PushFont(UiBuilder.IconFont);
        var size = ImGui.CalcTextSize(iconStr);
        ImGui.PopFont();

        var pos = new Vector2(origin.X + padX + (iconBox - size.X) * 0.5f, origin.Y + (height - size.Y) * 0.5f);
        dl.AddText(UiBuilder.IconFont, ImGui.GetFontSize(), pos, colour, iconStr);
    }

    private static void DrawLabel(ImDrawListPtr dl, string label, Vector2 origin, float offsetX, float height, uint colour)
    {
        var size = ImGui.CalcTextSize(label);
        dl.AddText(new Vector2(origin.X + offsetX, origin.Y + (height - size.Y) * 0.5f), colour, label);
    }

    private static float DrawTrailingIcon(ImDrawListPtr dl, FontAwesomeIcon icon, float rightX, float originY, float height, uint colour)
    {
        var iconStr = icon.ToIconString();
        ImGui.PushFont(UiBuilder.IconFont);
        var size = ImGui.CalcTextSize(iconStr);
        ImGui.PopFont();

        var pos = new Vector2(rightX - size.X, originY + (height - size.Y) * 0.5f);
        dl.AddText(UiBuilder.IconFont, ImGui.GetFontSize(), pos, colour, iconStr);
        return pos.X;
    }

    private static void DrawTrailingPill(ImDrawListPtr dl, string text, Vector4 background, float rightX, float originY, float height, float scale)
    {
        var textSize = ImGui.CalcTextSize(text);
        var dotRadius = PillDotRadius * scale;
        var padX = PillPaddingX * scale;
        var padY = PillPaddingY * scale;
        var gap = PillDotGap * scale;

        var pillSize = new Vector2(
            padX * 2f + dotRadius * 2f + gap + textSize.X,
            textSize.Y + padY * 2f);

        var pillMin = new Vector2(rightX - pillSize.X, originY + (height - pillSize.Y) * 0.5f);

        dl.AddRectFilled(pillMin, pillMin + pillSize, ImGui.ColorConvertFloat4ToU32(background), pillSize.Y * 0.5f);

        var foreground = ImGui.ColorConvertFloat4ToU32(PillForeground);
        var dotCentre = new Vector2(pillMin.X + padX + dotRadius, pillMin.Y + pillSize.Y * 0.5f);
        dl.AddCircleFilled(dotCentre, dotRadius, foreground);
        dl.AddText(new Vector2(dotCentre.X + dotRadius + gap, pillMin.Y + padY), foreground, text);
    }
}
