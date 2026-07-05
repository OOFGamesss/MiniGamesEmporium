using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Settings tab for Higher/Lower.</summary>

namespace MiniGamesEmporium.Games.HigherLower.UI.Tabs;
public sealed class HigherLowerSettingsTab
{
    private readonly PluginConfiguration config;

    public HigherLowerSettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.HigherLowerOrange, "Higher/Lower");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        HigherLowerPreSessionSettingsFields.Draw(this.config);
    }
}
