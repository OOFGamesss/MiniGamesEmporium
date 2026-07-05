using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the global plugin Settings tab.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class SettingsTab
{
    private readonly PluginConfiguration config;
    public SettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }
    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Plugin Settings");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "BAR 777");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Display Name");
        Bar777PreSessionSettingsFields.DrawGameNameSetting(this.config);
    }
}
