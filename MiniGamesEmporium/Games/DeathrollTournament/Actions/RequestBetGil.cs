using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Sends the /tell requesting a bettor pay to confirm their bet on a target.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class RequestBetGil
{
    public static void Execute(string bettorEntry, string targetEntry, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var betUnit = config.DeathrollTournamentSession?.BetUnitAtStart
                      ?? config.DeathrollSession?.BetUnit
                      ?? config.DeathrollTournament.BetUnit;
        var msg = config.DeathrollTournament.Chat.RequestBetGilMessage
            .Replace("{player}",    bettorEntry.Trim())
            .Replace("{bettarget}", PlayerInfoService.StripWorld(targetEntry))
            .Replace("{betunit}",   betUnit.ToString("N0"));
        chatQueue.Enqueue(msg);
    }
}
