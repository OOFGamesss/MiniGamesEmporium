using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Reminds a queued player by /tell when they near the front of the queue.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnounceReminderToPlay
{
    public static void Execute(string rawPlayerEntry, int queuePosition, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.ReminderToPlayMessage,
            config,
            rawPlayerEntry,
            queuePosition);
        chatQueue.Enqueue(msg);
    }
}
