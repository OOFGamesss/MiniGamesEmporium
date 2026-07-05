using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Tells the buyer the entry cost and which registered player they are paying for.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class RequestGilBuyer
{
    public static void Execute(string buyerName, string playerEntry, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var strippedPlayer = PlayerInfoService.StripWorld(playerEntry);
        var entryCost = config.DeathrollTournamentSession?.EntryCostAtStart
                        ?? config.DeathrollSession?.EntryCost
                        ?? config.DeathrollTournament.EntryCost;
        var msg = config.DeathrollTournament.Chat.RequestGilBuyerMessage
            .Replace("{buyername}", buyerName)
            .Replace("{player}",    strippedPlayer)
            .Replace("{entrycost}", entryCost.ToString("N0"));
        chatQueue.Enqueue(msg);
    }
}
