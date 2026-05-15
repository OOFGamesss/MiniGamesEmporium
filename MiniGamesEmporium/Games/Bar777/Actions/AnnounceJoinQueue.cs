using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends a confirmation message to a player when they successfully join the BAR 777 queue, including their current queue position.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnounceJoinQueue
{
    public static void Execute(string rawPlayerEntry, int queuePosition, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.JoinQueueMessage,
            config,
            rawPlayerEntry,
            queuePosition);
        chatQueue.Enqueue(msg);
    }
}
