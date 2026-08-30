using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the Deathroll Tournament advertisement message to drum up sign-ups.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class Advertise
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var template = config.DeathrollTournament.Chat.AdvertiseMessage;
        if (string.IsNullOrWhiteSpace(template))
            return;

        var lines = template.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            chatQueue.Enqueue(DeathrollMessageFormatter.Format(line, config));
        }
    }
}
