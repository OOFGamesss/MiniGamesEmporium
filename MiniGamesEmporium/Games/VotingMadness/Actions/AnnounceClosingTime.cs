using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Services;

/// <summary>Enqueues the Voting Madness closing time announcement.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Actions;
public static class AnnounceClosingTime
{
    public static void Execute(PluginConfiguration config, VotingMadnessService service, ChatQueueService chatQueue)
    {
        var msg = VotingMadnessMessageFormatter.Format(config.VotingMadness.Chat.AnnounceClosingTimeMessage, config, service);
        if (!string.IsNullOrWhiteSpace(msg)) chatQueue.Enqueue(msg);
    }
}
