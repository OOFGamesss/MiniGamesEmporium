using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Tells a player their entry fee has landed and they may begin rolling.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class AnnouncePaymentReceived
{
    public static void Execute(string playerName, int rollMax, int attempts, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = CoinCollectorMessageFormatter.Format(
            config.CoinCollector.Chat.PaymentReceivedMessage,
            config,
            playerName: playerName,
            rollMax:    rollMax,
            attempt:    1,
            attempts:   attempts);
        chatQueue.Enqueue(msg);
    }
}
