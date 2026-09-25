using System.Collections.Generic;
using System.Linq;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Fills Coin Collector message templates with live session values.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class CoinCollectorMessageFormatter
{
    private const string NoStandingsText = "no one yet";

    public static string Format(
        string template,
        PluginConfiguration config,
        string? playerName = null,
        int queuePosition = -1,
        int? coins = null,
        long? totalPotOverride = null,
        string? keywordOverride = null,
        string? buyerName = null,
        int? rollMax = null,
        int? coinsToLead = null,
        long? winningAmount = null,
        int wrongRollMax = 0,
        int? attempt = null,
        int? attempts = null)
    {
        var session      = config.CoinCollectorActiveSession;
        var paidAttempts = config.CoinCollector.Attempts;
        var player       = playerName ?? MessageFormat.LivePlayer(session);
        var coinCount    = coins ?? config.CoinCollectorSession?.CoinsCollected ?? 0;
        var nextMax      = rollMax ?? CoinCollectorService.ComputeNextRollCommandMax(config);
        var attemptNo    = attempt ?? paidAttempts.AttemptsUsed;
        var attemptCount = attempts ?? paidAttempts.AttemptsPurchased;
        var totalPot     = totalPotOverride ?? CoinCollectorService.ComputeTotalPot(config);
        var share        = winningAmount ?? CoinCollectorService.ComputeWinnerShare(config, totalPot);
        var toLead       = coinsToLead ?? CoinCollectorService.ComputeLeadTarget(config, coinCount);
        var display      = MessageFormat.DisplayPlayer(template, player);
        var position     = queuePosition >= 0
            ? queuePosition
            : CoinCollectorService.ComputeStandingPosition(config, player);
        return template
            .Replace("{buyername}",     buyerName ?? session?.PaidByPlayerName ?? string.Empty)
            .Replace("{player}",        display)
            .Replace("{position}",      MessageFormat.Position(position))
            .Replace("{cost}",          config.CoinCollector.EntryCost.ToString("N0"))
            .Replace("{coins}",         coinCount.ToString())
            .Replace("{totalpot}",      totalPot.ToString("N0"))
            .Replace("{winningamount}", share.ToString("N0"))
            .Replace("{keyword}",       keywordOverride ?? config.QueueKeyword)
            .Replace("{rollmax}",       nextMax > 0 ? nextMax.ToString() : string.Empty)
            .Replace("{highestcoins}",  CoinCollectorService.ComputeTopScore(config).ToString())
            .Replace("{coinstolead}",   toLead.ToString())
            .Replace("{wrongmax}",      wrongRollMax > 0 ? wrongRollMax.ToString() : string.Empty)
            .Replace("{attempt}",       attemptNo > 0 ? attemptNo.ToString() : "1")
            .Replace("{attempts}",      attemptCount > 0 ? attemptCount.ToString() : "1")
            .Replace("{leader}",        BuildLeaderText(config))
            .Replace("{leaderboard}",   BuildLeaderboardText(config));
    }

    private static string BuildLeaderText(PluginConfiguration config)
    {
        var leaders = CoinCollectorService.ComputeStandings(config)
            .Where(r => r.IsEffectiveWinner)
            .Select(r => PlayerInfoService.StripWorld(r.PlayerName))
            .ToList();
        return leaders.Count > 0 ? string.Join(", ", leaders) : NoStandingsText;
    }

    private static string BuildLeaderboardText(PluginConfiguration config)
    {
        var standings = CoinCollectorService.ComputeStandings(config);
        if (standings.Count == 0) return NoStandingsText;

        var cap   = config.CoinCollector.Chat.LeaderboardNamesInMessage;
        var limit = cap > 0 ? cap : standings.Count;
        var parts = new List<string>();
        for (var i = 0; i < standings.Count && i < limit; i++)
            parts.Add($"{i + 1}. {PlayerInfoService.StripWorld(standings[i].PlayerName)} ({standings[i].BestScore})");
        if (standings.Count > limit)
            parts.Add($"+{standings.Count - limit} more");
        return string.Join(", ", parts);
    }
}
