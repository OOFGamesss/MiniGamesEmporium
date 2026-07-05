using System;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Shared keyword-join detection: matches an enabled channel and keyword, resolves the sender to a queue name, and excludes the host.</summary>

namespace MiniGamesEmporium.Utility;
public static class QueueKeywordMatcher
{
    public static string? TryResolveJoiner(QueueConfig channels, string keyword, XivChatType kind, SeString? sender, string message, PlayerInfoService playerInfo)
    {
        if (!channels.Matches(kind)) return null;
        if (string.IsNullOrWhiteSpace(keyword)) return null;
        if (!message.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return null;
        if (sender == null) return null;

        var queueName = PlayerInfoService.BuildQueueName(sender);
        if (string.IsNullOrWhiteSpace(queueName)) return null;

        if (playerInfo.IsHost(queueName)) return null;

        return queueName;
    }
}
