using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Utility;
using System;

/// <summary>Reusable closing time (Server Time) hour and minute inputs for session doors and settings.</summary>

namespace MiniGamesEmporium.UI.Components;
public static class CloseTimeFields
{
    public static void Draw(string suffix, float fieldWidth, ref int closeHour, ref int closeMinute, Action onChanged)
    {
        var enabled = closeHour >= 0;
        ImGui.TextDisabled("Closing Time (Server Time)");
        ImGui.Spacing();
        if (ImGui.Checkbox($"Set a closing time##CloseEnable_{suffix}", ref enabled))
        {
            if (enabled)
            {
                var (h, m) = ServerTimeUtil.SuggestCloseTime();
                closeHour   = h;
                closeMinute = m;
            }
            else
            {
                closeHour = -1;
            }
            onChanged();
        }
        if (!enabled) return;
        ImGui.Indent();
        var hour   = closeHour;
        var minute = closeMinute;
        if (DrawHourMinute(suffix, fieldWidth, ref hour, ref minute))
        {
            closeHour   = Math.Clamp(hour, 0, 23);
            closeMinute = Math.Clamp(minute, 0, 59);
            onChanged();
        }
        ImGui.Unindent();
    }

    private static bool DrawHourMinute(string suffix, float fieldWidth, ref int hour, ref int minute)
    {
        var changed = false;
        var avail   = fieldWidth > 0f ? fieldWidth : ImGui.GetContentRegionAvail().X;
        var half    = MathF.Max(80f, (avail - ImGui.GetStyle().ItemSpacing.X) * 0.5f);
        ImGui.TextDisabled("Hour (0-23)");
        ImGui.SameLine(half + ImGui.GetStyle().ItemSpacing.X);
        ImGui.TextDisabled("Minute (0-59)");
        ImGui.SetNextItemWidth(half);
        if (ImGui.InputInt($"##CloseHour_{suffix}", ref hour, 1, 1))
        {
            hour = Math.Clamp(hour, 0, 23);
            changed = true;
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(half);
        if (ImGui.InputInt($"##CloseMinute_{suffix}", ref minute, 1, 5))
        {
            minute = Math.Clamp(minute, 0, 59);
            changed = true;
        }
        return changed;
    }
}
