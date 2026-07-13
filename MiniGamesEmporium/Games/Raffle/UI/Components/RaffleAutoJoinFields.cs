using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Renders the auto join keyword toggle and inputs shared by the start door and settings tab.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Components;
public static class RaffleAutoJoinFields
{
    public static void Draw(PluginConfiguration config, string imguiSuffix, float fieldWidth)
    {
        var cfg = config.Raffle;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Auto Join");
        ImGui.Spacing();
        var autoJoin = cfg.AutoJoinKeyword;
        if (ImGui.Checkbox($"Auto Join Keyword##RaffleAutoJoin_{imguiSuffix}", ref autoJoin))
        {
            cfg.AutoJoinKeyword = autoJoin;
            config.Save();
        }
        if (!autoJoin) return;
        ImGui.Spacing();
        ImGui.Indent();
        var keyword = cfg.JoinKeyword;
        ImGui.TextDisabled("Join keyword");
        ImGui.SetNextItemWidth(fieldWidth);
        if (ImGui.InputText($"##RaffleJoinKeyword_{imguiSuffix}", ref keyword, 32))
        {
            cfg.JoinKeyword = keyword;
            config.Save();
        }
        ImGui.Spacing();
        ImGui.TextDisabled("Listen on chats");
        ImGui.SetNextItemWidth(fieldWidth);
        if (QueueChannelCombo.Draw($"RaffleJoinListen_{imguiSuffix}", cfg.JoinChannels))
            config.Save();
        ImGui.Unindent();
    }
}
