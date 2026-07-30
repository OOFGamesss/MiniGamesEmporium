using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Shared chat keyword detection used by the game keyword handlers.</summary>

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

    public static string? TryResolveVoter(QueueConfig channels, XivChatType kind, SeString? sender, PlayerInfoService playerInfo)
    {
        if (!channels.Matches(kind)) return null;
        if (sender == null) return null;

        var queueName = PlayerInfoService.BuildQueueName(sender);
        if (string.IsNullOrWhiteSpace(queueName)) return null;
        if (playerInfo.IsHost(queueName)) return null;
        return queueName;
    }

    public static IReadOnlyList<(string Keyword, int Index)> FindWholeWordKeywords(string message, IEnumerable<string> keywords)
    {
        var found = new List<(string Keyword, int Index)>();
        foreach (var keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword)) continue;
            var index = IndexOfWholeWord(message, keyword);
            if (index >= 0)
                found.Add((keyword, index));
        }
        return found.OrderBy(m => m.Index).ToList();
    }

    private static string? TryResolveSender(QueueConfig channels, string keyword, XivChatType kind, SeString? sender, string message, PlayerInfoService playerInfo, out int keywordIndex)
    {
        keywordIndex = -1;
        if (!channels.Matches(kind)) return null;
        if (string.IsNullOrWhiteSpace(keyword)) return null;
        keywordIndex = IndexOfWholeWord(message, keyword);
        if (keywordIndex < 0) return null;
        if (sender == null) return null;

        var queueName = PlayerInfoService.BuildQueueName(sender);
        if (string.IsNullOrWhiteSpace(queueName)) return null;
        if (playerInfo.IsHost(queueName)) return null;

        return queueName;
    }

    private static int IndexOfWholeWord(string message, string keyword)
    {
        var start = 0;
        while (start <= message.Length - keyword.Length)
        {
            var index = message.IndexOf(keyword, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return -1;
            if (IsWholeWordAt(message, keyword, index))
                return index;
            start = index + 1;
        }
        return -1;
    }

    private static bool IsWholeWordAt(string message, string keyword, int index)
    {
        if (index > 0 && IsWordChar(message[index - 1]))
            return false;
        var after = index + keyword.Length;
        if (after < message.Length && IsWordChar(message[after]))
            return false;
        return true;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

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
