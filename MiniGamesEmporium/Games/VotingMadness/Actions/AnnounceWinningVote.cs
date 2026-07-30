using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Services;

/// <summary>Enqueues the winning vote or tie announcement for Voting Madness.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Actions;
public static class AnnounceWinningVote
{
    public static void Execute(PluginConfiguration config, VotingMadnessService service, ChatQueueService chatQueue)
    {
        var (_, _, _, isTie) = service.GetResult();
        var template = isTie
            ? config.VotingMadness.Chat.AnnounceTieMessage
            : config.VotingMadness.Chat.AnnounceWinningVoteMessage;
        var msg = VotingMadnessMessageFormatter.Format(template, config, service);
        if (!string.IsNullOrWhiteSpace(msg)) chatQueue.Enqueue(msg);
    }
}
