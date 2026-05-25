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
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, this.config.Bar777.CustomName);
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
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "Automation");
        ImGui.Spacing();
        DrawAutoCatchRollToggle();
    }

    private void DrawAutoCatchRollToggle()
    {
        var autoCatch = this.config.Bar777.AutoCatchRoll;
        if (ImGui.Checkbox("Auto Catch Roll##Bar777AutoCatch", ref autoCatch))
        {
            this.config.Bar777.AutoCatchRoll = autoCatch;
            this.config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("- auto-starts the game when the current player rolls /random after paying");
    }
}
