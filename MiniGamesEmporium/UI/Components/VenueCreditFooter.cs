using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using System;
using System.IO;
using System.Numerics;

/// <summary>Draws a right-aligned venue suggestion credit in the bottom corner of a game view.</summary>

namespace MiniGamesEmporium.UI.Components;
internal sealed class VenueCreditFooter
{
    private const float LogoSize = 34f;

    private readonly string creditText;
    private readonly ISharedImmediateTexture? logo;

    internal VenueCreditFooter(string venueImageFileName, string venueName)
    {
        this.creditText = $"Game suggested by {venueName}";
        var path = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "Venues", venueImageFileName);
        if (File.Exists(path))
            this.logo = MiniGamesEmporium.TextureProvider.GetFromFile(path);
    }

    internal static float RowHeight() =>
        (LogoSize * ImGuiHelpers.GlobalScale) + ImGui.GetStyle().ItemSpacing.Y;

    internal void Draw()
    {
        var side    = LogoSize * ImGuiHelpers.GlobalScale;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var textW   = ImGui.CalcTextSize(this.creditText).X;
        var rowW    = textW + spacing + side;
        var rowTopY = ImGui.GetCursorPosY();
        var startX  = ImGui.GetCursorPosX() + MathF.Max(0f, ImGui.GetContentRegionAvail().X - rowW);

        ImGui.SetCursorPos(new Vector2(
            startX,
            rowTopY + MathF.Max(0f, (side - ImGui.GetTextLineHeight()) * 0.5f)));
        ImGui.TextDisabled(this.creditText);
        ImGui.SameLine(0f, spacing);
        ImGui.SetCursorPosY(rowTopY);

        var wrap = this.logo?.GetWrapOrDefault();
        if (wrap != null)
            ImGui.Image(wrap.Handle, new Vector2(side, side));
        else
            ImGui.Dummy(new Vector2(side, side));
    }
}
