using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the halfway message to chat when a player has used exactly half of their allotted rolls.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnounceHalfway
{
    public static void Execute(string playerName, int rollsRemaining, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.HalfwayMessage,
            config,
            playerName,
            remainingOverride: rollsRemaining.ToString());
        chatQueue.Enqueue(msg);
    }
}
