using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the global plugin settings sections.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class SettingsTab
{
    private const float FieldWidth = 320f;
    private const int GameKeyBufferLength = 128;

    private readonly PluginConfiguration config;
    public SettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }

    public void DrawWebviewSetupSection()
    {
        ImGui.Spacing();
        UIHelper.SectionHeader("Web Spectator", EmporiumNeonTheme.NeonCyan);
        ImGui.TextWrapped("Mirror games to the OOF Games website so anyone can watch live in a browser.");
        ImGuiHelpers.ScaledDummy(8f);

        DrawGameKeySection();
    }

    private void DrawGameKeySection()
    {
        UIHelper.SectionHeader("Game Key", EmporiumNeonTheme.NeonCyan);

        var key = this.config.Webview.ApiHostKey;

        ImGui.SetNextItemWidth(FieldWidth * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText("##mge_game_key", ref key, GameKeyBufferLength, ImGuiInputTextFlags.Password))
        {
            this.config.Webview.ApiHostKey = key.Trim();
            this.config.Save();
        }

        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Clear", "##mge_clear_game_key"))
        {
            this.config.Webview.ApiHostKey = string.Empty;
            this.config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Clear the saved game key from this plugin.");

        ImGui.SameLine();
        ImGui.TextDisabled("No key yet? See Support > Game Key.");

        ImGuiHelpers.ScaledDummy(8f);
        ImGui.TextDisabled("Then open a game's Webview section (e.g. Deathroll Tournament) to go live.");
    }

    public void DrawOtherSettingsSection()
    {
        ImGui.Spacing();
        UIHelper.SectionHeader("Other Settings", EmporiumNeonTheme.NeonCyan);
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "BAR 777");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Display Name");
        Bar777PreSessionSettingsFields.DrawGameNameSetting(this.config);
    }
}
