using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Shared chat keyword detection: matches an enabled channel and keyword, resolves the sender to a queue name and excludes the host, and for bets extracts the target name that follows the keyword.</summary>

namespace MiniGamesEmporium.Utility;
public static class KeywordMatcher
{
    public sealed class BetResult
    {
        public string BettorQueueName { get; init; } = string.Empty;
        public string RawTargetText { get; init; } = string.Empty;
        public string? ResolvedTargetName { get; init; }
    }

    public static string? TryResolveJoiner(QueueConfig channels, string keyword, XivChatType kind, SeString? sender, string message, PlayerInfoService playerInfo) =>
        TryResolveSender(channels, keyword, kind, sender, message, playerInfo, out _);

    public static BetResult? TryResolveBet(QueueConfig channels, string keyword, XivChatType kind, SeString? sender, string message, PlayerInfoService playerInfo, IEnumerable<string> knownTargetNames)
    {
        var bettorQueueName = TryResolveSender(channels, keyword, kind, sender, message, playerInfo, out var keywordIndex);
        if (bettorQueueName == null) return null;

        var rawTarget = message[(keywordIndex + keyword.Length)..].Trim();
        var resolved  = string.IsNullOrWhiteSpace(rawTarget) ? null : TryFuzzyResolve(rawTarget, knownTargetNames);
        return new BetResult
        {
            BettorQueueName    = bettorQueueName,
            RawTargetText      = rawTarget,
            ResolvedTargetName = resolved,
        };
    }

    private static string? TryResolveSender(QueueConfig channels, string keyword, XivChatType kind, SeString? sender, string message, PlayerInfoService playerInfo, out int keywordIndex)
    {
        keywordIndex = -1;
        if (!channels.Matches(kind)) return null;
        if (string.IsNullOrWhiteSpace(keyword)) return null;
        keywordIndex = message.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (keywordIndex < 0) return null;
        if (sender == null) return null;

        var queueName = PlayerInfoService.BuildQueueName(sender);
        if (string.IsNullOrWhiteSpace(queueName)) return null;
        if (playerInfo.IsHost(queueName)) return null;

        return queueName;
    }

    private static string? TryFuzzyResolve(string typedText, IEnumerable<string> candidates)
    {
        var strippedTyped = PlayerInfoService.StripWorld(typedText);
        var list          = candidates.ToList();

        var exact = list.FirstOrDefault(c => PlayerInfoService.StripWorld(c).Equals(strippedTyped, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        var startsWith = list.Where(c => PlayerInfoService.StripWorld(c).StartsWith(strippedTyped, StringComparison.OrdinalIgnoreCase)).ToList();
        return startsWith.Count == 1 ? startsWith[0] : null;
    }
}
