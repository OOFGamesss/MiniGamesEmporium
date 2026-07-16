using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.UI.Components;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the global plugin Settings tab.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class SettingsTab
{
    private const string OofGamesDiscordUrl = "https://discord.gg/vM6ff4h5Ym";

    private readonly PluginConfiguration config;
    public SettingsTab(PluginConfiguration config)
    {
        this.config = config;
    }
    public void Draw()
    {
        ImGui.Spacing();
        using var tabBar = ImRaii.TabBar("##MGE_Settings_TabBar");
        if (!tabBar.Success) return;
        DrawWebviewSetupTab();
        DrawOtherSettingsTab();
    }

    private void DrawWebviewSetupTab()
    {
        using var tab = ImRaii.TabItem("Webview Setup");
        if (!tab.Success) return;
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Webview Setup");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Mirror games to the OOF Games website so anyone can watch live in a browser.");
        ImGui.Spacing();

        var key = this.config.Webview.ApiHostKey;
        ImGui.TextUnformatted("Host API Key:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(320);
        if (ImGui.InputText("##mge_api_host_key", ref key, 128, ImGuiInputTextFlags.Password))
        {
            this.config.Webview.ApiHostKey = key.Trim();
            this.config.Save();
        }
        ImGui.SameLine();
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Get an API key FREE from the OOF Games Discord.");
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("##mge_open_discord_api_key", FontAwesomeIcon.Globe))
            Util.OpenLink(OofGamesDiscordUrl);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Open in browser:\n{OofGamesDiscordUrl}");
        ImGui.Spacing();
        ImGui.TextDisabled("Then open a game's Webview tab (e.g. Deathroll Tournament) to go live.");
    }

    private void DrawOtherSettingsTab()
    {
        using var tab = ImRaii.TabItem("Other Settings");
        if (!tab.Success) return;
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Other Settings");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "BAR 777");
        ImGui.SameLine(0, 4);
        ImGui.TextUnformatted("Display Name");
        Bar777PreSessionSettingsFields.DrawGameNameSetting(this.config);
    }
}
