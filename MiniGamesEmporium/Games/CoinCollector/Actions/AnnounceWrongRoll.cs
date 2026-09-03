using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Tells a player their roll used the wrong maximum and which number to roll instead.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class AnnounceWrongRoll
{
    public static void Execute(string playerName, int wrongRollMax, int expectedRollMax, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = CoinCollectorMessageFormatter.Format(
            config.CoinCollector.Chat.WrongRollMessage,
            config,
            playerName:   playerName,
            rollMax:      expectedRollMax,
            wrongRollMax: wrongRollMax);
        chatQueue.Enqueue(msg);
    }
}
