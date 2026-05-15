using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;
using System;
using System.Text;

/// <summary>Renders the queue join keyword text input and the listened chat channel selection combo, shared between the pre-session door and the settings tab.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Components;
public static class Bar777QueueKeywordFields
{
    public static void Draw(PluginConfiguration config, string imguiSuffix)
    {
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
        var ch = config.QueueJoinChannels;
        if (!ImGui.BeginCombo($"##Bar777QueueListen_{imguiTrimSuffix}", FormatChannelPreview(ch)))
            return;
        SayRow(config, ch, imguiTrimSuffix);
        ShoutRow(config, ch, imguiTrimSuffix);
        YellRow(config, ch, imguiTrimSuffix);
        TellIncomingRow(config, ch, imguiTrimSuffix);
        ImGui.EndCombo();
    }
    private static void SayRow(PluginConfiguration cfg, QueueJoinChannelsConfig ch, string su)
    {
        bool v = ch.Say;
        if (!ImGui.Checkbox($"/say##ql_{su}s", ref v))
            return;
        ch.Say = v;
        cfg.Save();
    }
    private static void ShoutRow(PluginConfiguration cfg, QueueJoinChannelsConfig ch, string su)
    {
        bool v = ch.Shout;
        if (!ImGui.Checkbox($"/shout##ql_{su}sh", ref v))
            return;
        ch.Shout = v;
        cfg.Save();
    }
    private static void YellRow(PluginConfiguration cfg, QueueJoinChannelsConfig ch, string su)
    {
        bool v = ch.Yell;
        if (!ImGui.Checkbox($"/yell##ql_{su}y", ref v))
            return;
        ch.Yell = v;
        cfg.Save();
    }
    private static void TellIncomingRow(PluginConfiguration cfg, QueueJoinChannelsConfig ch, string su)
    {
        bool v = ch.TellIncoming;
        if (!ImGui.Checkbox($"/tell (incoming only)##ql_{su}t", ref v))
            return;
        ch.TellIncoming = v;
        cfg.Save();
    }
    private static string FormatChannelPreview(QueueJoinChannelsConfig c)
    {
        if (!c.AnyEnabled())
            return "(none selected)";
        var sb = new StringBuilder(48);
        if (c.Say) Append(sb, "/say");
        if (c.Shout) Append(sb, "/shout");
        if (c.Yell) Append(sb, "/yell");
        if (c.TellIncoming) Append(sb, "/tell");
        return sb.ToString();
        static void Append(StringBuilder sb, string part)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(part);
        }
    }
}
