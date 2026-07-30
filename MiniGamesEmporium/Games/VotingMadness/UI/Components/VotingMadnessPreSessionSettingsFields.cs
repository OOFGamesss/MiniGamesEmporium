using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.UI.Components;

/// <summary>Renders Voting Madness pre-session fields shared by the start door and settings tab.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.UI.Components;
public static class VotingMadnessPreSessionSettingsFields
{
    private const int MinOptions = 2;

    public static void Draw(PluginConfiguration config, string suffix = "Door", float fieldWidth = 0f)
    {
        var cfg = config.VotingMadness;
        EnsureMinOptions(cfg.Options);

        ImGui.TextDisabled("Voting Options");
        ImGui.Spacing();
        var removeIdx = -1;
        var wrapEnd = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        for (var i = 0; i < cfg.Options.Count; i++)
        {
            var option = cfg.Options[i];
            var isDuplicate = IsDuplicateOption(cfg.Options, i);
            ImGui.SetNextItemWidth(fieldWidth > 0f ? fieldWidth - 36f : -36f);
            if (ImGui.InputText($"##VMOption_{suffix}_{i}", ref option, 48))
            {
                cfg.Options[i] = option;
                config.Save();
            }
            ImGui.SameLine();
            using (ImRaii.Disabled(cfg.Options.Count <= MinOptions))
            {
                if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, string.Empty, $"##VMRemoveOption_{suffix}_{i}"))
                    removeIdx = i;
            }
            if (isDuplicate)
            {
                ImGui.PushTextWrapPos(wrapEnd);
                ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Duplicate option text.");
                ImGui.PopTextWrapPos();
            }
        }
        if (removeIdx >= 0 && cfg.Options.Count > MinOptions)
        {
            cfg.Options.RemoveAt(removeIdx);
            config.Save();
        }
        ImGui.Spacing();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Add Option", $"##VMAddOption_{suffix}"))
        {
            cfg.Options.Add(string.Empty);
            config.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("Listen on chats");
        ImGui.Spacing();
        ImGui.SetNextItemWidth(fieldWidth > 0f ? fieldWidth : -1f);
        if (QueueChannelCombo.Draw($"VMVoteListen_{suffix}", cfg.VoteChannels))
            config.Save();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("Vote Rules");
        ImGui.Spacing();
        wrapEnd = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
        var multipleChoice = cfg.MultipleChoice;
        if (ImGui.Checkbox($"Multiple choice##VMMulti_{suffix}", ref multipleChoice))
        {
            cfg.MultipleChoice = multipleChoice;
            config.Save();
        }
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextDisabled("Players may vote for more than one option (one vote per option), across messages.");
        ImGui.PopTextWrapPos();

        var allowMultiple = cfg.AllowMultipleVotes;
        if (ImGui.Checkbox($"Allow multiple votes per person##VMAllowMulti_{suffix}", ref allowMultiple))
        {
            cfg.AllowMultipleVotes = allowMultiple;
            config.Save();
        }
        ImGui.PushTextWrapPos(wrapEnd);
        ImGui.TextDisabled("Players may vote for the same option more than once.");
        ImGui.PopTextWrapPos();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var hour   = cfg.CloseHour;
        var minute = cfg.CloseMinute;
        CloseTimeFields.Draw($"VM_{suffix}", fieldWidth, ref hour, ref minute, () =>
        {
            cfg.CloseHour   = hour;
            cfg.CloseMinute = minute;
            config.Save();
        });
        cfg.CloseHour   = hour;
        cfg.CloseMinute = minute;
    }

    public static bool CanStart(PluginConfiguration config)
    {
        var options = config.VotingMadness.Options
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToList();
        if (options.Count < MinOptions) return false;
        if (HasDuplicateOptions(options)) return false;
        return config.VotingMadness.VoteChannels.AnyEnabled();
    }

    public static string? GetStartBlockReason(PluginConfiguration config)
    {
        var options = config.VotingMadness.Options
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToList();
        if (options.Count < MinOptions)
            return "Add at least two options and select at least one chat channel.";
        if (HasDuplicateOptions(options))
            return null;
        if (!config.VotingMadness.VoteChannels.AnyEnabled())
            return "Select at least one chat channel.";
        return null;
    }

    public static string? GetDuplicateOptionsTooltip(PluginConfiguration config)
    {
        var duplicates = config.VotingMadness.Options
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .GroupBy(o => o, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count == 0) return null;
        return "Duplicate options:\n" + string.Join("\n", duplicates.Select(d => $"• {d}"));
    }

    private static bool HasDuplicateOptions(IReadOnlyList<string> options) =>
        options.GroupBy(o => o, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1);

    private static bool IsDuplicateOption(IReadOnlyList<string> options, int index)
    {
        var value = options[index]?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;
        for (var i = 0; i < options.Count; i++)
        {
            if (i == index) continue;
            if (string.Equals(options[i]?.Trim(), value, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void EnsureMinOptions(List<string> options)
    {
        while (options.Count < MinOptions)
            options.Add(string.Empty);
    }
}
