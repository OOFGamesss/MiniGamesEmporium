using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Broadcasts the multi-line Higher/Lower rules, sending each line as its own spaced chat message.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class AnnounceRules
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue)
    {
        var template = config.HigherLower.Chat.RulesMessage;
        if (string.IsNullOrWhiteSpace(template))
            return;

        var lines = template.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var msg = HigherLowerMessageFormatter.Format(line, config);
            chatQueue.Enqueue(msg);
        }
    }
}
