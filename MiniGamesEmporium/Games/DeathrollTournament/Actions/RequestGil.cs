using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable Request Gil tell message to a registered player, substituting their full name (including world) and the entry cost.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class RequestGil
{
    public static void Execute(string playerEntry, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var entryCost = config.DeathrollTournamentSession?.EntryCostAtStart
                        ?? config.DeathrollSession?.EntryCost
                        ?? config.DeathrollTournament.EntryCost;
        var msg = config.DeathrollTournament.Chat.RequestGilMessage
            .Replace("{player}",    playerEntry.Trim())
            .Replace("{entrycost}", entryCost.ToString("N0"));
        chatQueue.Enqueue(msg);
    }
}
