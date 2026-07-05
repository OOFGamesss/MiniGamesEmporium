using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Tells the buyer the Gil cost and which player they are paying for.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class SendTellBuyerRequest
{
    public static void Execute(string buyerName, string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var strippedPlayer = PlayerInfoService.StripWorld(playerName);
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.TellBuyerRequestMessage,
            config,
            strippedPlayer,
            buyerName: buyerName);
        chatQueue.Enqueue(msg);
    }
}
