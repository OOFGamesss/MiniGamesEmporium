using Dalamud.Bindings.ImGui;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.UI.Components;
using System;

/// <summary>Renders the raffle closing time (Server Time) hour and minute inputs for the start door and live session.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Components;
public static class RaffleCloseTimeFields
{
    public static void DrawConfig(PluginConfiguration config, string suffix, float fieldWidth)
    {
        var cfg     = config.Raffle;
        var enabled = cfg.CloseHour >= 0;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Closing Time (Server Time)");
        ImGui.Spacing();
        if (ImGui.Checkbox($"Set a closing time##RaffleCloseEnable_{suffix}", ref enabled))
        {
            if (enabled)
            {
                var (h, m) = RaffleTimeUtil.SuggestCloseTime();
                cfg.CloseHour   = h;
                cfg.CloseMinute = m;
            }
            else
            {
                cfg.CloseHour = -1;
            }
            config.Save();
        }
        if (!enabled) return;
        ImGui.Indent();
        var hour   = cfg.CloseHour;
        var minute = cfg.CloseMinute;
        if (DrawHourMinute(suffix, fieldWidth, ref hour, ref minute))
        {
            cfg.CloseHour   = Math.Clamp(hour, 0, 23);
            cfg.CloseMinute = Math.Clamp(minute, 0, 59);
            config.Save();
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
        if (ImGui.InputInt($"##RaffleCloseHour_{suffix}", ref hour, 1, 1))
        {
            hour = Math.Clamp(hour, 0, 23);
            changed = true;
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(half);
        if (ImGui.InputInt($"##RaffleCloseMinute_{suffix}", ref minute, 1, 5))
        {
            minute = Math.Clamp(minute, 0, 59);
            changed = true;
        }
        return changed;
    }
}
