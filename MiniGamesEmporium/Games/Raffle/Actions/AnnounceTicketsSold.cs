using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable tickets-sold announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class AnnounceTicketsSold
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = RaffleMessageFormatter.Format(config.Raffle.Chat.AnnounceTicketsSoldMessage, config);
        chatQueue.Enqueue(msg);
    }
}
