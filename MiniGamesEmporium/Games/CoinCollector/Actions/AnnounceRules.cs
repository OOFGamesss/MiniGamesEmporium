using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the multi-line Coin Collector rules, sending each line as its own spaced chat message.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Actions;
public static class AnnounceRules
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var template = config.CoinCollector.Chat.RulesMessage;
        if (string.IsNullOrWhiteSpace(template))
            return;

        var lines = template.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            chatQueue.Enqueue(CoinCollectorMessageFormatter.Format(line, config));
        }
    }
}
