using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Yells the queue-join keyword so players know how to enter the BAR 777 queue.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnounceKeyword
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(config.Bar777.Chat.AnnounceKeywordMessage, config);
        chatQueue.Enqueue(msg);
    }
}
