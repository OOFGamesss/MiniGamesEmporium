using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using System;
using System.Text;

/// <summary>Reusable "listen on chats" dropdown (say/shout/yell/tell) backed by a QueueConfig.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class QueueChannelCombo
{
    /// <summary>Draws the channel checklist combo. Returns true when a channel toggled.
    /// The caller sets the item width beforehand and persists the config on a true return.</summary>
    public static bool Draw(string id, QueueConfig channels)
    {
        if (!ImGui.BeginCombo($"##{id}", FormatPreview(channels)))
            return false;
        var changed = false;
        changed |= Row($"/say##{id}_say", channels.Say, v => channels.Say = v);
        changed |= Row($"/shout##{id}_shout", channels.Shout, v => channels.Shout = v);
        changed |= Row($"/yell##{id}_yell", channels.Yell, v => channels.Yell = v);
        changed |= Row($"/tell (incoming only)##{id}_tell", channels.TellIncoming, v => channels.TellIncoming = v);
        ImGui.EndCombo();
        return changed;
    }

    private static bool Row(string label, bool value, Action<bool> setter)
    {
        var v = value;
        if (!ImGui.Checkbox(label, ref v))
            return false;
        setter(v);
        return true;
    }

    public static string FormatPreview(QueueConfig c)
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
