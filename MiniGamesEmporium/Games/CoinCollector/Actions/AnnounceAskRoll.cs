using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured message telling the current player which dice command to roll next.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class AnnounceAskRoll
{
    public static void Execute(string playerName, int rollMax, int coins, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var chat = config.CoinCollector.Chat;
        var template = coins > 0 ? chat.AskRollWithCoinsMessage : chat.AskRollMessage;
        var msg = CoinCollectorMessageFormatter.Format(
            template,
            config, playerName: playerName, coins: coins, rollMax: rollMax);
        chatQueue.Enqueue(msg);
    }
}
