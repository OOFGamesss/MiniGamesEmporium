using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Linq;
using System.Numerics;

/// <summary>Draws the shared "Available Placeholders" token and description list used by every game's chat settings tab.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class PlaceholderReference
{
    private static readonly Vector4 TokenColour = new(1f, 0.80f, 0.30f, 1f);
    private const float ColumnGap = 20f;

    public static void Draw((string Token, string Desc)[] rows)
    {
        if (rows == null || rows.Length == 0) return;
        var descColX = ImGui.GetCursorPosX()
                     + rows.Max(r => ImGui.CalcTextSize(r.Token).X)
                     + ColumnGap * ImGuiHelpers.GlobalScale;
        foreach (var (token, desc) in rows)
            DrawRow(token, desc, descColX);
    }

    private static void DrawRow(string token, string desc, float descColX)
    {
        ImGui.TextColored(TokenColour, token);
        ImGui.SameLine(descColX);
        using var wrap = ImRaii.TextWrapPos(0f);
        ImGui.TextDisabled(desc);
    }
}
