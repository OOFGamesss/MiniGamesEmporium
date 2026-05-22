using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable first-player announcement to chat once the order rolls have resolved, naming the player who goes first in the deathroll.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceFirstPlayer
{
    public static void Execute(string firstPlayer, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.AnnounceFirstPlayerMessage,
            config,
            firstPlayer: firstPlayer);
        chatQueue.Enqueue(msg);
    }
}
