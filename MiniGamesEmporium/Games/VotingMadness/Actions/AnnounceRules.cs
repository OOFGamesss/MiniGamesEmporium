using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the multi-line Voting Madness rules, sending each line as its own spaced chat message.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Actions;
public static class AnnounceRules
{
    public static void Execute(PluginConfiguration config, VotingMadnessService service, ChatQueueService chatQueue)
    {
        var template = config.VotingMadness.Chat.RulesMessage;
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
