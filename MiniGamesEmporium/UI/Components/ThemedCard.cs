using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

/// <summary>Draws a rounded, tinted container with a centred title, sized to its content.</summary>

namespace MiniGamesEmporium.UI.Components;
public sealed class ThemedCard
{
    private const float GapBelow = 8f;
    private const float Rounding = 6f;
    private const float PadX     = 22f;
    private const float PadY     = 12f;
    private const float Margin   = 8f;
    private const float Border   = 1.5f;

    private readonly Dictionary<string, float> heights = new(StringComparer.Ordinal);

    private static Vector4 Background(Vector4 accent) => new(accent.X, accent.Y, accent.Z, 0.12f);

    private static Vector4 BorderColour(Vector4 accent) => new(accent.X, accent.Y, accent.Z, 0.55f);

    public void Draw(string id, string title, Vector4 accent, Vector4 titleColour, Action content) =>
        this.Draw(id, title, accent, titleColour, 0f, content);

    public void Draw(string id, string title, Vector4 accent, Vector4 titleColour, float fixedHeight, Action content)
    {
        var scale    = ImGuiHelpers.GlobalScale;
        var rounding = Rounding * scale;
        var padX     = PadX * scale;
        var padY     = PadY * scale;
        var margin   = Margin * scale;

        var startX = ImGui.GetCursorPosX();
        var availW = ImGui.GetContentRegionAvail().X;
        var cardW  = MathF.Max(60f * scale, availW - margin * 2f);

        ImGui.SetCursorPosX(startX + margin);
        var top = ImGui.GetCursorScreenPos();

        var bodyH = fixedHeight > 0f
            ? fixedHeight
            : this.heights.TryGetValue(id, out var cached) ? cached : 4f * scale;

        using (ImRaii.PushColor(ImGuiCol.ChildBg, Background(accent)))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, rounding))
        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(padX, padY)))
        {
            using var child = ImRaii.Child(id, new Vector2(cardW, bodyH), false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysUseWindowPadding);
            if (child.Success)
            {
                var topY = ImGui.GetCursorPosY();
                DrawTitle(title, titleColour);
                content();
                this.heights[id] = ImGui.GetCursorPosY() - topY + padY * 2f;
            }
        }

        var bottom = ImGui.GetCursorScreenPos().Y;
        ImGui.GetWindowDrawList().AddRect(
            top,
            new Vector2(top.X + cardW, bottom),
            ImGui.GetColorU32(BorderColour(accent)),
            rounding,
            ImDrawFlags.None,
            Border * scale);

        ImGui.SetCursorPosX(startX);
        ImGuiHelpers.ScaledDummy(GapBelow);
    }

    public float MatchedHeight(params string[] ids)
    {
        var tallest = 0f;
        foreach (var id in ids)
            if (this.heights.TryGetValue(id, out var height) && height > tallest)
                tallest = height;
        return tallest;
    }

    public static float ChromeHeight()
    {
        var scale = ImGuiHelpers.GlobalScale;
        return PadY * 2f * scale + GapBelow * scale;
    }

    private static void DrawTitle(string title, Vector4 titleColour)
    {
        if (string.IsNullOrEmpty(title)) return;
        var availW = ImGui.GetContentRegionAvail().X;
        var textW  = ImGui.CalcTextSize(title).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, (availW - textW) * 0.5f));
        ImGui.TextColored(titleColour, title);
        ImGuiHelpers.ScaledDummy(2f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(4f);
    }
}
