using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Services;

/// <summary>Enqueues the current Voting Madness standings announcement.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Actions;
public static class AnnounceStandings
{
    public static void Execute(PluginConfiguration config, VotingMadnessService service, ChatQueueService chatQueue)
    {
        var msg = VotingMadnessMessageFormatter.Format(config.VotingMadness.Chat.AnnounceStandingsMessage, config, service);
        if (!string.IsNullOrWhiteSpace(msg)) chatQueue.Enqueue(msg);
    }
}
