using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends a reminder via /tell to a queued player when their position reaches the configured threshold, prompting them to come and play.</summary>

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
