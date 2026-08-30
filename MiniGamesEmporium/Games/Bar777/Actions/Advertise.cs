using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the BAR 777 advertisement message to drum up new players.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class Advertise
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var template = config.Bar777.Chat.AdvertiseMessage;
        if (string.IsNullOrWhiteSpace(template))
            return;

        var lines = template.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            chatQueue.Enqueue(Bar777MessageFormatter.Format(line, config));
        }
    }
}
