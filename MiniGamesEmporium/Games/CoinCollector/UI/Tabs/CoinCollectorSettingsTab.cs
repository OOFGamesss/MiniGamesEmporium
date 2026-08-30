using System.Numerics;
using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Settings tab for Coin Collector.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.UI.Tabs;
public sealed class CoinCollectorSettingsTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.CoinCollectorIndigo;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private readonly PluginConfiguration config;

    public CoinCollectorSettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(CardAccent, "Coin Collector");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Settings");
        ImGui.Separator();
        ImGui.Spacing();
        if (this.config.CoinCollectorActiveSession != null)
        {
            SessionLockNotice.Draw(
                "A session is active. Session defaults are locked until you stop the session.");
            return;
        }
        this.card.Draw("##CCSettingsCard", "Session Defaults", CardAccent, CardTitle, DrawSettingsBody);
    }

    private void DrawSettingsBody()
    {
        CoinCollectorPreSessionSettingsFields.Draw(this.config);
    }
}
