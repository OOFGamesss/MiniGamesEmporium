using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Services;
using MiniGamesEmporium.Utility;

/// <summary>Fills Coin Collector message templates with live session values.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class CoinCollectorMessageFormatter
{
    public static string Format(
        string template,
        PluginConfiguration config,
        string playerName = "",
        int queuePosition = 0,
        int coins = 0,
        long? totalPotOverride = null,
        string? keywordOverride = null,
        string buyerName = "",
        int rollMax = 0,
        int highestCoins = 0,
        long winningAmount = 0)
    {
        var totalPot = totalPotOverride ?? CoinCollectorService.ComputeTotalPot(config);
        var display  = MessageFormat.DisplayPlayer(template, playerName);
        return template
            .Replace("{buyername}",     buyerName)
            .Replace("{player}",        display)
            .Replace("{position}",      MessageFormat.Position(queuePosition))
            .Replace("{cost}",          config.CoinCollector.EntryCost.ToString("N0"))
            .Replace("{coins}",         coins.ToString())
            .Replace("{totalpot}",      totalPot.ToString("N0"))
            .Replace("{winningamount}", winningAmount.ToString("N0"))
            .Replace("{keyword}",       keywordOverride ?? config.QueueKeyword)
            .Replace("{rollmax}",       rollMax > 0 ? rollMax.ToString() : string.Empty)
            .Replace("{highestcoins}",  highestCoins.ToString());
    }
}
