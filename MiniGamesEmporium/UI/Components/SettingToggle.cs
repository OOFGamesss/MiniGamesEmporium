using Dalamud.Bindings.ImGui;

/// <summary>Draws a labelled checkbox with a trailing description, the shared setting row for every game.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class SettingToggle
{
    public static bool Draw(string label, string id, string description, ref bool value)
    {
        var changed = ImGui.Checkbox($"{label}{id}", ref value);
        if (!string.IsNullOrEmpty(description))
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"- {description}");
        }
        return changed;
    }

    public static bool DrawIntField(string label, string id, string description, ref int value, int min, int max, int step, float width)
    {
        ImGui.TextDisabled(label);
        ImGui.SetNextItemWidth(width);
        var changed = ImGui.InputInt(id, ref value, step, step * 10);
        if (changed)
            value = System.Math.Clamp(value, min, max);
        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);
        return changed;
    }
}
