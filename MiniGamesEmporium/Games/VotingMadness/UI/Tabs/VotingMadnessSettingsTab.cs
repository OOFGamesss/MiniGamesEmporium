using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Settings tab for Voting Madness.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.UI.Tabs;
public sealed class VotingMadnessSettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.VotingMadnessLime;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;

    public VotingMadnessSettingsTab(PluginConfiguration config) => this.config = config;

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Voting Madness");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        if (this.config.VotingMadnessSession != null)
        {
            SessionLockNotice.Draw(
                "A session is active. Options and rules are locked until you stop the session.");
            return;
        }
        this.card.Draw("##VMSettingsCard", "Session Defaults", CardAccent, CardTitle, DrawSettingsBody);
    }

    private void DrawSettingsBody() =>
        VotingMadnessPreSessionSettingsFields.Draw(this.config, "Settings");
}
