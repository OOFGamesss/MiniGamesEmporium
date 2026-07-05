using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured message asking the current player to guess higher or lower than the rolled number.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class AnnounceAskGuess
{
    public static void Execute(string playerName, int rolledNumber, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = HigherLowerMessageFormatter.Format(
            config.HigherLower.Chat.AskGuessMessage,
            config, playerName: playerName, rolledNumber: rolledNumber);
        chatQueue.Enqueue(msg);
    }
}
