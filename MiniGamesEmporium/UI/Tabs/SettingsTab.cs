using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the global plugin Settings tab, currently displaying informational text directing users to the per-game settings panels within the Mini Games tab.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class SettingsTab
{
    public SettingsTab(PluginConfiguration config)
    {
        _ = config;
    }
    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Plugin Settings");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled(
            "BAR 777 rules, queue keyword, and session controls live under Mini Games.");
    }
}
