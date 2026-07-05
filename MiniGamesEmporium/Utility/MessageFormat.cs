using System;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Services;

/// <summary>Shared helpers for substituting common placeholders in game chat message templates.</summary>

namespace MiniGamesEmporium.Utility;
public static class MessageFormat
{
    public static string DisplayPlayer(string template, string playerName) =>
        template.TrimStart().StartsWith("/tell", StringComparison.OrdinalIgnoreCase)
            ? playerName
            : PlayerInfoService.StripWorld(playerName);

    public static string Position(int queuePosition) =>
        queuePosition == 0 ? "next" : $"#{queuePosition}";

    public static void CopyTellToClipboard(string playerNameWithWorld, IChatGui chatGui)
    {
        var (name, world) = PlayerInfoService.SplitNameAndWorld(playerNameWithWorld);
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(world))
            return;

        var target = $"{name}@{world}";
        ImGui.SetClipboardText($"/tell {target} ");

        var msg = new SeStringBuilder()
            .AddText($"Copied \"/tell {target}\" to your clipboard. Paste it into chat to message them.")
            .Build();
        chatGui.Print(msg, "MiniGamesEmporium");
    }
}
