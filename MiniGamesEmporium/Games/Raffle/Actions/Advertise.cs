using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the Raffle advertisement message to drum up new ticket buyers.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class Advertise
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var template = config.Raffle.Chat.AdvertiseMessage;
        if (string.IsNullOrWhiteSpace(template))
            return;

        var lines = template.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            chatQueue.Enqueue(RaffleMessageFormatter.Format(line, config));
        }
    }
}
