using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable join-reminder announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class AnnounceJoinReminder
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = RaffleMessageFormatter.Format(config.Raffle.Chat.AnnounceJoinReminderMessage, config);
        chatQueue.Enqueue(msg);
    }
}
