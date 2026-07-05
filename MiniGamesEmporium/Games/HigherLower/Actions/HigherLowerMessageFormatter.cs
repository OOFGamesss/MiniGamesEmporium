using MiniGamesEmporium.Config;
using MiniGamesEmporium.Utility;

/// <summary>Fills Higher/Lower message templates with live session values.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class HigherLowerMessageFormatter
{
    public static string Format(
        string template,
        PluginConfiguration config,
        string playerName = "",
        int queuePosition = 0,
        int rounds = 0,
        long? totalPotOverride = null,
        string? keywordOverride = null,
        string buyerName = "",
        int rolledNumber = 0,
        int highestRound = 0,
        long winningAmount = 0)
    {
        var totalPot   = totalPotOverride ?? config.HigherLower.ComputeTotalPot();
        var display    = MessageFormat.DisplayPlayer(template, playerName);
        return template
            .Replace("{buyername}",    buyerName)
            .Replace("{player}",       display)
            .Replace("{position}",     MessageFormat.Position(queuePosition))
            .Replace("{cost}",         config.HigherLower.EntryCost.ToString("N0"))
            .Replace("{rounds}",       rounds.ToString())
            .Replace("{totalpot}",     totalPot.ToString("N0"))
            .Replace("{winningamount}", winningAmount.ToString("N0"))
            .Replace("{keyword}",      keywordOverride ?? config.QueueKeyword)
            .Replace("{rollednumber}", rolledNumber.ToString())
            .Replace("{highestround}", highestRound.ToString());
    }
}
