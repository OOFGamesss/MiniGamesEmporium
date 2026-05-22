using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configurable re-roll /random 10 announcement message to chat when both players tie their order roll.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class AnnounceRerollRandom10
{
    public static void Execute(int tiedValue, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = DeathrollMessageFormatter.Format(
            config.DeathrollTournament.Chat.RerollRandom10Message,
            config,
            random10: tiedValue);
        chatQueue.Enqueue(msg);
    }
}
