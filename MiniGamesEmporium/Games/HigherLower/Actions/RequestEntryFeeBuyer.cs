using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Tells the buyer the Higher/Lower entry cost and which player they are paying for.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class RequestEntryFeeBuyer
{
    public static void Execute(string buyerName, string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = HigherLowerMessageFormatter.Format(
            config.HigherLower.Chat.RequestGilBuyerMessage,
            config,
            playerName: PlayerInfoService.StripWorld(playerName),
            buyerName: buyerName.Trim());
        chatQueue.Enqueue(msg);
    }
}
