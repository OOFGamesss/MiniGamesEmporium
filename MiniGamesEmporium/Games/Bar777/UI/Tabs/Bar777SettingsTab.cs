using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Settings tab for BAR 777, exposing the pre-session game configuration fields and, when queue mode is enabled, the queue keyword options.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Tabs;
public sealed class Bar777SettingsTab
{
    private readonly PluginConfiguration config;
    public Bar777SettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }
    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "BAR 777");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        Bar777PreSessionSettingsFields.Draw(this.config);
        if (this.config.Bar777.UseQueue)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            Bar777QueueKeywordFields.Draw(this.config, "SettingsTab");
        }
    }
}
