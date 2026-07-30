using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Renders the queue keyword and channel fields shared by the door and settings tab.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Components;
public static class Bar777QueueKeywordFields
{
    public static void Draw(PluginConfiguration config, string imguiSuffix)
    {
        ImGui.TextDisabled("Queue");
        ImGui.Spacing();
        ImGui.TextDisabled("Queue join keyword");
        ImGui.Spacing();
        var keyword = config.QueueKeyword;
        ImGui.SetNextItemWidth(ResolvedFieldWidth());
        if (ImGui.InputText($"Chat keyword##Bar777QueueKw_{imguiSuffix}", ref keyword, 32))
        {
            config.QueueKeyword = keyword;
            config.Save();
        }
        ImGui.Spacing();
        DrawListenChannelsCombo(config, imguiTrimSuffix: imguiSuffix);
        ImGui.Spacing();
        var wrapEnd = ImGui.GetCursorPos().X + Math.Max(8f, ImGui.GetContentRegionAvail().X);
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextDisabled(
            "Characters who post this keyword using the chats you picked are queued. Optional waitlist only.");
        ImGui.PopTextWrapPos();
    }
    private static float ResolvedFieldWidth()
    {
        var w = ImGui.GetContentRegionAvail().X;
        return w > 48f ? w : EmporiumNeonTheme.StartDoorPanelWidth;
    }
    private static void DrawListenChannelsCombo(PluginConfiguration config, string imguiTrimSuffix)
    {
        ImGui.TextDisabled("Listen on chats");
        ImGui.SetNextItemWidth(ResolvedFieldWidth());
        if (QueueChannelCombo.Draw($"Bar777QueueListen_{imguiTrimSuffix}", config.QueueJoinChannels))
            config.Save();
    }
}
