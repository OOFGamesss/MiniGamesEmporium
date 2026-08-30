using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured win shout message for a Coin Collector session winner, including their share of the pot.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class AnnounceCoinCollectorWin
{
    public static void Execute(string playerName, int coins, long totalPot, long winningAmount, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = CoinCollectorMessageFormatter.Format(
            config.CoinCollector.Chat.WinShoutMessage,
            config,
            playerName: playerName,
            coins: coins,
            totalPotOverride: totalPot,
            winningAmount: winningAmount);
        chatQueue.Enqueue(msg);
    }
}
