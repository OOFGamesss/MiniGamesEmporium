using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Tells the current player how much Gil to trade to enter Coin Collector.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class RequestEntryFee
{
    public static void Execute(string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = CoinCollectorMessageFormatter.Format(
            config.CoinCollector.Chat.TellAmountRequestMessage,
            config, playerName: playerName);
        chatQueue.Enqueue(msg);
    }
}
