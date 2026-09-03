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
        string playerName = "",
        int queuePosition = -1,
        int coins = 0,
        long? totalPotOverride = null,
        string? keywordOverride = null,
        string buyerName = "",
        int rollMax = 0,
        int highestCoins = 0,
        long winningAmount = 0,
        int wrongRollMax = 0,
        int attempt = 0,
        int attempts = 0)
    {
        var totalPot = totalPotOverride ?? CoinCollectorService.ComputeTotalPot(config);
        var display  = MessageFormat.DisplayPlayer(template, playerName);
        var position = queuePosition >= 0
            ? queuePosition
            : CoinCollectorService.ComputeStandingPosition(config, playerName);
        return template
            .Replace("{buyername}",     buyerName)
            .Replace("{player}",        display)
            .Replace("{position}",      MessageFormat.Position(position))
            .Replace("{cost}",          config.CoinCollector.EntryCost.ToString("N0"))
            .Replace("{coins}",         coins.ToString())
            .Replace("{totalpot}",      totalPot.ToString("N0"))
            .Replace("{winningamount}", winningAmount.ToString("N0"))
            .Replace("{keyword}",       keywordOverride ?? config.QueueKeyword)
            .Replace("{rollmax}",       rollMax > 0 ? rollMax.ToString() : string.Empty)
            .Replace("{highestcoins}",  highestCoins.ToString())
            .Replace("{wrongmax}",      wrongRollMax > 0 ? wrongRollMax.ToString() : string.Empty)
            .Replace("{attempt}",       attempt > 0 ? attempt.ToString() : "1")
            .Replace("{attempts}",      attempts > 0 ? attempts.ToString() : "1")
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
