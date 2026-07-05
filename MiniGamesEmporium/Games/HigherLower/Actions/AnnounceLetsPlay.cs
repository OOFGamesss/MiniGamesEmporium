using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured "let's play" message to invite the current player to begin their turn.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class AnnounceLetsPlay
{
    public static void Execute(string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = HigherLowerMessageFormatter.Format(
            config.HigherLower.Chat.LetsPlayMessage,
            config, playerName: playerName);
        chatQueue.Enqueue(msg);
    }
}
