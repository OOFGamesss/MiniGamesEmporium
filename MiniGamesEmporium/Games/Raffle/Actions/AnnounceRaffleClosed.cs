using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable raffle-closed announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class AnnounceRaffleClosed
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = RaffleMessageFormatter.Format(config.Raffle.Chat.AnnounceRaffleClosedMessage, config);
        chatQueue.Enqueue(msg);
    }
}
