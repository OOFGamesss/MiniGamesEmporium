using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the start-rolls message once payment is verified.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnouncePaymentReceived
{
    public static void Execute(string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(config.Bar777.Chat.StartRollsMessage, config, playerName);
        chatQueue.Enqueue(msg);
    }
}
