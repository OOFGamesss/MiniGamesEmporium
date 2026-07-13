using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable closing-time announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class AnnounceClosingTime
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = RaffleMessageFormatter.Format(config.Raffle.Chat.AnnounceClosingTimeMessage, config);
        chatQueue.Enqueue(msg);
    }
}
