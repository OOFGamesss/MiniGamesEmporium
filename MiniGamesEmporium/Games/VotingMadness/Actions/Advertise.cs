using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the Voting Madness advertisement message to drum up more voters.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Actions;
public static class Advertise
{
    public static void Execute(PluginConfiguration config, VotingMadnessService service, ChatQueueService chatQueue)
    {
        var template = config.VotingMadness.Chat.AdvertiseMessage;
        if (string.IsNullOrWhiteSpace(template))
            return;

        var lines = template.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            chatQueue.Enqueue(VotingMadnessMessageFormatter.Format(line, config, service));
        }
    }
}
