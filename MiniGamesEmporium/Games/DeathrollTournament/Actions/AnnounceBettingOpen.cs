using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable betting-open announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceBettingOpen
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.AnnounceBettingOpenMessage,
            config);
        chatQueue.Enqueue(msg);
    }
}
