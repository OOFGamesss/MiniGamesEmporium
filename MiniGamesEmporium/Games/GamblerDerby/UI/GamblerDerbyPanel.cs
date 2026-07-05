using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.UI.Components;
using System;
using System.Numerics;

/// <summary>Placeholder panel for the Gambler Derby game, showing a coming-soon message.</summary>

namespace MiniGamesEmporium.Games.GamblerDerby.UI;
public sealed class GamblerDerbyPanel
{
    private const string ComingSoonMessage = "This game is coming soon!";
    private const float FontScale = 2.0f;
    public void Draw()
    {
        ImGui.SetWindowFontScale(FontScale);
        var textSize = ImGui.CalcTextSize(ComingSoonMessage);
        ImGui.SetWindowFontScale(1.0f);
        var avail = ImGui.GetContentRegionAvail();
        var cursorBase = ImGui.GetCursorPos();
        var x = cursorBase.X + MathF.Max(0f, (avail.X - textSize.X) * 0.5f);
        var y = cursorBase.Y + MathF.Max(0f, (avail.Y - textSize.Y) * 0.5f);
        ImGui.SetCursorPos(new Vector2(x, y));
        ImGui.SetWindowFontScale(FontScale);
        ImGui.TextColored(EmporiumNeonTheme.GamblerDerbyYellow, ComingSoonMessage);
        ImGui.SetWindowFontScale(1.0f);
    }
}
