using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured score or lead message when a Coin Collector player busts on a 1.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class AnnounceCoinCollectorLoss
{
    public static void ExecuteLeaderboardAnnounce(
        string playerName,
        int coins,
        bool isLeading,
        int highestCoins,
        PluginConfiguration config,
        ChatQueueService chatQueue)
    {
        var template = isLeading
            ? config.CoinCollector.Chat.LossWinningMessage
            : config.CoinCollector.Chat.LossUnluckyMessage;
        var msg = CoinCollectorMessageFormatter.Format(
            template,
            config,
            playerName:   playerName,
            coins:        coins,
            highestCoins: highestCoins);
        chatQueue.Enqueue(msg);
    }
}
