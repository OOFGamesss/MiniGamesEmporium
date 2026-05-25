using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends a queue announcement to chat naming the next player up for BAR 777.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnounceNextPlayerUp
{
    public static void Execute(string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.NextPlayerUpMessage,
            config,
            playerName);
        chatQueue.Enqueue(msg);
    }
}
