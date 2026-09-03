using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Tells the buyer the Coin Collector entry cost and which player they are paying for.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class RequestEntryFeeBuyer
{
    public static void Execute(string buyerName, string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = CoinCollectorMessageFormatter.Format(
            config.CoinCollector.Chat.RequestGilBuyerMessage,
            config,
            playerName: PlayerInfoService.StripWorld(playerName),
            buyerName:  buyerName.Trim());
        chatQueue.Enqueue(msg);
        if (config.CoinCollector.TradeOnRequestGil)
            SendTradeRequest.Execute(PlayerInfoService.StripWorld(buyerName), chatQueue);
    }
}
