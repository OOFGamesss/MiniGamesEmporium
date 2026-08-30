using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Announces the current Coin Collector pot total.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class AnnouncePot
{
    public static void Execute(long totalPot, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = CoinCollectorMessageFormatter.Format(
            config.CoinCollector.Chat.AnnouncePotMessage,
            config, totalPotOverride: totalPot);
        chatQueue.Enqueue(msg);
    }
}
