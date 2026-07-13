using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the Request Gil tell to a registered player with the ticket cost.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class RequestGil
{
    public static void Execute(string playerEntry, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = RaffleMessageFormatter.Format(
            config.Raffle.Chat.RequestGilMessage, config, player: playerEntry.Trim());
        chatQueue.Enqueue(msg);
    }
}
