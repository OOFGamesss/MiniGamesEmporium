using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable prize announcement message to chat when triggered from the stats panel or a session start.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnouncePrize
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = DeathrollMessageFormatter.Format(config.DeathrollTournament.Chat.AnnouncePrizeMessage, config);
        chatQueue.Enqueue(msg);
    }
}
