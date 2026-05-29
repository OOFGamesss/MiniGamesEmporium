using MiniGamesEmporium.Config;
using System;

/// <summary>Formats BAR 777 chat message templates by substituting placeholders such as {player}, {cost}, {maxcost}, {rolls}, {remaining}, {totalpot}, and {keyword} with live session and configuration values.</summary>

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
        var totalPot = totalPotOverride ?? config.Bar777.BoostedPot + config.Bar777.SessionTradedTotal;
        var remaining = remainingOverride
            ?? (session != null
                ? Math.Max(0, session.RollsAllowed - session.RollsUsed).ToString()
                : rollsAllowed.ToString());
        var isTell = template.TrimStart().StartsWith("/tell", StringComparison.OrdinalIgnoreCase);
        var displayPlayer = isTell ? playerName : StripWorld(playerName);
        return template
            .Replace("{buyername}",   buyerName)
            .Replace("{player}",      displayPlayer)
            .Replace("{position}",    queuePosition == 0 ? "next" : $"#{queuePosition}")
            .Replace("{cost}",        config.Bar777.CostPerRoll.ToString("N0"))
            .Replace("{maxcost}",    (config.Bar777.CostPerRoll * config.Bar777.MaxRolls).ToString("N0"))
            .Replace("{rolls}",       rollsAllowed.ToString())
            .Replace("{boughtrolls}", boughtRolls.ToString())
            .Replace("{remaining}",   remaining)
            .Replace("{totalpot}",    totalPot.ToString("N0"))
            .Replace("{keyword}",     keywordOverride ?? config.QueueKeyword);
    }

    private static string StripWorld(string name)
    {
        var at = name.IndexOf('@');
        return at >= 0 ? name[..at].Trim() : name.Trim();
    }
}
