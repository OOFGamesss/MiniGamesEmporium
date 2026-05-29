using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace MiniGamesEmporium.UI.Components;

/// <summary>
/// Shared UI rendering helpers used across all tab components.
/// </summary>
internal static class UIHelper
{
    internal static bool IconTextButton(FontAwesomeIcon icon, string label, string id = "", float minWidth = 0f)
    {
        var iconFont    = UiBuilder.IconFont;
        var defaultFont = ImGui.GetFont();
        var fontSize    = ImGui.GetFontSize();
        var iconStr     = icon.ToIconString();
        var style       = ImGui.GetStyle();

        ImGui.PushFont(iconFont);
        var iconSize = ImGui.CalcTextSize(iconStr);
        ImGui.PopFont();

        var textSize = ImGui.CalcTextSize(label);
        var height   = Math.Max(iconSize.Y, textSize.Y);
        var contentW = textSize.X > 0f
            ? iconSize.X + style.ItemInnerSpacing.X + textSize.X
            : iconSize.X;
        var naturalW = style.FramePadding.X * 2 + contentW;

        var btnSize = new Vector2(
            Math.Max(naturalW, minWidth),
            style.FramePadding.Y * 2 + height);

        var btnId   = string.IsNullOrEmpty(id) ? $"##{label}" : id;
        var clicked = ImGui.InvisibleButton(btnId, btnSize);

        var isHovered = ImGui.IsItemHovered();
        var isActive  = ImGui.IsItemActive();

        var bgColor = isActive  ? ImGui.GetColorU32(ImGuiCol.ButtonActive)
                    : isHovered ? ImGui.GetColorU32(ImGuiCol.ButtonHovered)
                    :             ImGui.GetColorU32(ImGuiCol.Button);

        var dl  = ImGui.GetWindowDrawList();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();

        dl.AddRectFilled(min, max, bgColor, style.FrameRounding);

        if (style.FrameBorderSize > 0f)
            dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border), style.FrameRounding,
                ImDrawFlags.None, style.FrameBorderSize);

        var textColor = ImGui.GetColorU32(ImGuiCol.Text);

        var innerW      = btnSize.X - style.FramePadding.X * 2;
        var contentOffX = style.FramePadding.X + (innerW - contentW) * 0.5f;
        var iconOffsetY = (height - iconSize.Y) * 0.5f;
        var textOffsetY = (height - textSize.Y) * 0.5f;

        var iconPos = new Vector2(min.X + contentOffX, min.Y + style.FramePadding.Y + iconOffsetY);
        dl.AddText(iconFont, fontSize, iconPos, textColor, iconStr);

        if (textSize.X > 0f)
        {
            var textPos = new Vector2(iconPos.X + iconSize.X + style.ItemInnerSpacing.X,
                                      min.Y + style.FramePadding.Y + textOffsetY);
            dl.AddText(defaultFont, fontSize, textPos, textColor, label);
        }

        return clicked;
    }

    internal static Vector2 CalcButtonSize(FontAwesomeIcon icon, string label)
    {
        var iconFont = UiBuilder.IconFont;
        var iconStr  = icon.ToIconString();
        var style    = ImGui.GetStyle();

        ImGui.PushFont(iconFont);
        var iconSize = ImGui.CalcTextSize(iconStr);
        ImGui.PopFont();

        var textSize = ImGui.CalcTextSize(label);
        var height   = Math.Max(iconSize.Y, textSize.Y);

        var contentW = textSize.X > 0f
            ? iconSize.X + style.ItemInnerSpacing.X + textSize.X
            : iconSize.X;
        return new Vector2(
            style.FramePadding.X * 2 + contentW,
            style.FramePadding.Y * 2 + height);
    }

    internal static ImRaii.ColorDisposable PushGreenButtonColours() =>
        new ImRaii.ColorDisposable()
            .Push(ImGuiCol.Button,        new Vector4(0.1f,  0.6f,  0.1f,  1f))
            .Push(ImGuiCol.ButtonHovered, new Vector4(0.2f,  0.8f,  0.2f,  1f))
            .Push(ImGuiCol.ButtonActive,  new Vector4(0.05f, 0.4f,  0.05f, 1f));

    internal static ImRaii.ColorDisposable PushRedButtonColours() =>
        new ImRaii.ColorDisposable()
            .Push(ImGuiCol.Button,        new Vector4(0.6f,  0.1f,  0.1f,  1f))
            .Push(ImGuiCol.ButtonHovered, new Vector4(0.8f,  0.2f,  0.2f,  1f))
            .Push(ImGuiCol.ButtonActive,  new Vector4(0.4f,  0.05f, 0.05f, 1f));

    internal static ImRaii.ColorDisposable PushYellowButtonColours() =>
        new ImRaii.ColorDisposable()
            .Push(ImGuiCol.Button,        new Vector4(0.62f, 0.56f, 0.03f, 1f))
            .Push(ImGuiCol.ButtonHovered, new Vector4(0.78f, 0.72f, 0.04f, 1f))
            .Push(ImGuiCol.ButtonActive,  new Vector4(0.48f, 0.44f, 0.02f, 1f));

    internal static ImRaii.ColorDisposable PushBlueButtonColours() =>
        new ImRaii.ColorDisposable()
            .Push(ImGuiCol.Button,        new Vector4(0.10f, 0.36f, 0.72f, 1f))
            .Push(ImGuiCol.ButtonHovered, new Vector4(0.15f, 0.50f, 0.90f, 1f))
            .Push(ImGuiCol.ButtonActive,  new Vector4(0.08f, 0.28f, 0.55f, 1f));

    internal static ImRaii.ColorDisposable PushAmberButtonColours() =>
        new ImRaii.ColorDisposable()
            .Push(ImGuiCol.Button,        new Vector4(0.52f, 0.40f, 0.02f, 1f))
            .Push(ImGuiCol.ButtonHovered, new Vector4(0.72f, 0.58f, 0.03f, 1f))
            .Push(ImGuiCol.ButtonActive,  new Vector4(0.38f, 0.28f, 0.01f, 1f));

    internal static ImRaii.ColorDisposable PushOrangeButtonColours() =>
        new ImRaii.ColorDisposable()
            .Push(ImGuiCol.Button,        new Vector4(0.60f, 0.25f, 0.02f, 1f))
            .Push(ImGuiCol.ButtonHovered, new Vector4(0.80f, 0.35f, 0.03f, 1f))
            .Push(ImGuiCol.ButtonActive,  new Vector4(0.45f, 0.18f, 0.01f, 1f));

    internal static int DrawPagination(int currentPage, int totalPages, string id)
    {
        using (ImRaii.Disabled(currentPage == 0))
        {
            if (IconTextButton(FontAwesomeIcon.ChevronLeft, string.Empty, $"##Prev{id}"))
                return Math.Max(0, currentPage - 1);
        }
        ImGui.SameLine();
        ImGui.TextUnformatted($"Page {currentPage + 1} of {totalPages}");
        ImGui.SameLine();
        using (ImRaii.Disabled(currentPage >= totalPages - 1))
        {
            if (IconTextButton(FontAwesomeIcon.ChevronRight, string.Empty, $"##Next{id}"))
                return Math.Min(totalPages - 1, currentPage + 1);
        }
        return currentPage;
    }
}
