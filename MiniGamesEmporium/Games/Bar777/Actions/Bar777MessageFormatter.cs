using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Services;
using MiniGamesEmporium.Utility;

/// <summary>Fills BAR 777 message templates with live session values.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class Bar777MessageFormatter
{
    public static string Format(
        string template,
        PluginConfiguration config,
        string playerName = "",
        int queuePosition = 0,
        long? totalPotOverride = null,
        string? remainingOverride = null,
        string? keywordOverride = null,
        string buyerName = "")
    {
        var session = config.ActiveSession;
        var rollsAllowed = session?.RollsAllowed ?? config.Bar777.MaxRolls;
        var boughtRolls  = session?.RollsAllowed ?? 0;
        var totalPot = totalPotOverride ?? Bar777SessionService.ComputeTotalPot(config);
        var remaining = remainingOverride
            ?? (session != null
                ? Math.Max(0, session.RollsAllowed - session.RollsUsed).ToString()
                : rollsAllowed.ToString());
        var displayPlayer = MessageFormat.DisplayPlayer(template, playerName);
        return template
            .Replace("{buyername}",   buyerName)
            .Replace("{player}",      displayPlayer)
            .Replace("{position}",    MessageFormat.Position(queuePosition))
            .Replace("{cost}",        config.Bar777.CostPerRoll.ToString("N0"))
            .Replace("{maxcost}",    (config.Bar777.CostPerRoll * config.Bar777.MaxRolls).ToString("N0"))
            .Replace("{rolls}",       rollsAllowed.ToString())
            .Replace("{boughtrolls}", boughtRolls.ToString())
            .Replace("{remaining}",   remaining)
            .Replace("{totalpot}",    totalPot.ToString("N0"))
            .Replace("{keyword}",     keywordOverride ?? config.QueueKeyword);
    }

}
