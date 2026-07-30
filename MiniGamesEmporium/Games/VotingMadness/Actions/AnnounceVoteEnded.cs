using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Services;

/// <summary>Enqueues the Voting Madness vote-ended announcement.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Actions;
public static class AnnounceVoteEnded
{
    public static void Execute(PluginConfiguration config, VotingMadnessService service, ChatQueueService chatQueue)
    {
        var msg = VotingMadnessMessageFormatter.Format(config.VotingMadness.Chat.VoteEndedMessage, config, service);
        if (!string.IsNullOrWhiteSpace(msg)) chatQueue.Enqueue(msg);
    }
}
