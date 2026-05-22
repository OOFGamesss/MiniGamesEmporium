using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable pot announcement message to chat when triggered from the stats panel.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnouncePot
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue, long totalPot)
    {
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.AnnouncePotMessage,
            config,
            totalPotOverride: totalPot);
        chatQueue.Enqueue(msg);
    }
}
