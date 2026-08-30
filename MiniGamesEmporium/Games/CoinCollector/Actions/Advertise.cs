using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the Coin Collector advertisement message to drum up new players.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class Advertise
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var template = config.CoinCollector.Chat.AdvertiseMessage;
        if (string.IsNullOrWhiteSpace(template))
            return;

        var lines = template.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            chatQueue.Enqueue(CoinCollectorMessageFormatter.Format(line, config));
        }
    }
}
