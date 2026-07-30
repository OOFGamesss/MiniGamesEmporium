using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Settings tab for Voting Madness.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.UI.Tabs;
public sealed class VotingMadnessSettingsTab
{
    private readonly PluginConfiguration config;

    public VotingMadnessSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.VotingMadnessLime, "Voting Madness");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        if (this.config.VotingMadnessSession != null)
        {
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber,
                "A session is active. Options and rules are locked until you stop the session.");
            ImGui.Spacing();
            return;
        }
        VotingMadnessPreSessionSettingsFields.Draw(this.config, "Settings");
    }
}
