using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable pot announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class AnnouncePot
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue, long totalPot)
    {
        var msg = RaffleMessageFormatter.Format(
            config.Raffle.Chat.AnnouncePotMessage, config, totalPotOverride: totalPot);
        chatQueue.Enqueue(msg);
    }
}
