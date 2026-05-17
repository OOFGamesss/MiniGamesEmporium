using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured queue-join keyword announcement to the /yell channel so players know what to type to enter the BAR 777 queue.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnounceKeyword
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(config.Bar777.Chat.AnnounceKeywordMessage, config);
        chatQueue.Enqueue(msg);
    }
}
