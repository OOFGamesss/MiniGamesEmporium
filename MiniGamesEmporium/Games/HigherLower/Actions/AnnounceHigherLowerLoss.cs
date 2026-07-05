using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured loss message when a player guesses incorrectly in Higher/Lower.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class AnnounceHigherLowerLoss
{
    public static void ExecuteLeaderboardAnnounce(
        string playerName,
        int rounds,
        bool isLeading,
        int highestRound,
        PluginConfiguration config,
        ChatQueueService chatQueue)
    {
        var template = isLeading
            ? config.HigherLower.Chat.LossWinningMessage
            : config.HigherLower.Chat.LossUnluckyMessage;
        var msg = HigherLowerMessageFormatter.Format(
            template,
            config,
            playerName:   playerName,
            rounds:       rounds,
            highestRound: highestRound);
        chatQueue.Enqueue(msg);
    }
}
