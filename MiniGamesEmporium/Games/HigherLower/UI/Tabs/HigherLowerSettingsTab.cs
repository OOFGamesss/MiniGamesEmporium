using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Settings tab for Higher/Lower.</summary>

namespace MiniGamesEmporium.Games.HigherLower.UI.Tabs;
public sealed class HigherLowerSettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.HigherLowerOrange;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;

    public HigherLowerSettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Higher/Lower");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        if (this.config.HigherLowerActiveSession != null)
        {
            SessionLockNotice.Draw(
                "A session is active. Session defaults are locked until you stop the session.");
            return;
        }
        this.card.Draw("##HLSettingsCard", "Session Defaults", CardAccent, CardTitle, DrawSettingsBody);
    }

    private void DrawSettingsBody()
    {
        HigherLowerPreSessionSettingsFields.Draw(this.config);
    }
}
