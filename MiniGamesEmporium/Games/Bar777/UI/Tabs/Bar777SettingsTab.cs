using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Settings tab for BAR 777.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Tabs;
public sealed class Bar777SettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.Bar777Red;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;
    public Bar777SettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }
    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, this.config.Bar777.CustomName);
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        var locked = this.config.ActiveSession != null;
        if (locked)
            SessionLockNotice.Draw(
                "A session is active. Session defaults are locked until you stop the session.");
        else
            this.card.Draw("##B7SettingsCard", "Session Defaults", CardAccent, CardTitle,
                () => Bar777PreSessionSettingsFields.Draw(this.config));
        if (this.config.Bar777.UseQueue)
            this.card.Draw("##B7QueueCard", "Queue Keyword", CardAccent, CardTitle,
                () => Bar777QueueKeywordFields.Draw(this.config, "SettingsTab"));
        this.card.Draw("##B7AutomationCard", "Automation", CardAccent, CardTitle, DrawAutoCatchRollToggle);
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
