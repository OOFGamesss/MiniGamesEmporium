using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable betting pot announcement message to chat.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceBettingPot
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue, long bettingPot)
    {
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.AnnounceBettingPotMessage,
            config,
            bettingPotOverride: bettingPot);
        chatQueue.Enqueue(msg);
    }
}
