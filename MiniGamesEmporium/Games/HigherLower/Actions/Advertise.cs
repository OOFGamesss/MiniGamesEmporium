using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the Higher/Lower advertisement message to drum up new players.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class Advertise
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var template = config.HigherLower.Chat.AdvertiseMessage;
        if (string.IsNullOrWhiteSpace(template))
            return;

        var lines = template.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            chatQueue.Enqueue(HigherLowerMessageFormatter.Format(line, config));
        }
    }
}
