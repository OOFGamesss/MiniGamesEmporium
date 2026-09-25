using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Services;
using MiniGamesEmporium.Utility;

/// <summary>Fills Higher/Lower message templates with live session values.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class HigherLowerMessageFormatter
{
    public static string Format(
        string template,
        PluginConfiguration config,
        string? playerName = null,
        int queuePosition = 0,
        int? rounds = null,
        long? totalPotOverride = null,
        string? keywordOverride = null,
        string? buyerName = null,
        int? rolledNumber = null,
        int? roundsToLead = null,
        long? winningAmount = null)
    {
        var session    = config.HigherLowerActiveSession;
        var turn       = config.HigherLowerSession;
        var roundCount = rounds ?? turn?.RoundsCorrect ?? 0;
        var rolled     = rolledNumber ?? (turn?.RollLog.Count > 0 ? (int?)turn.RollLog[^1] : null);
        var totalPot   = totalPotOverride ?? HigherLowerService.ComputeTotalPot(config);
        var share      = winningAmount ?? HigherLowerService.ComputeWinnerShare(config, totalPot);
        var toLead     = roundsToLead ?? HigherLowerService.ComputeLeadTarget(config, roundCount);
        var display    = MessageFormat.DisplayPlayer(template, playerName ?? MessageFormat.LivePlayer(session));
        return template
            .Replace("{buyername}",    buyerName ?? session?.PaidByPlayerName ?? string.Empty)
            .Replace("{player}",       display)
            .Replace("{position}",     MessageFormat.Position(queuePosition))
            .Replace("{cost}",         config.HigherLower.EntryCost.ToString("N0"))
            .Replace("{rounds}",       roundCount.ToString())
            .Replace("{totalpot}",     totalPot.ToString("N0"))
            .Replace("{winningamount}", share.ToString("N0"))
            .Replace("{keyword}",      keywordOverride ?? config.QueueKeyword)
            .Replace("{rollednumber}", rolled?.ToString() ?? string.Empty)
            .Replace("{highestround}", HigherLowerService.ComputeTopRounds(config).ToString())
            .Replace("{roundstolead}", toLead.ToString());
    }
}
