using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Announces the current Higher/Lower pot total.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class AnnouncePot
{
    public static void Execute(long totalPot, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = HigherLowerMessageFormatter.Format(
            config.HigherLower.Chat.AnnouncePotMessage,
            config, totalPotOverride: totalPot);
        chatQueue.Enqueue(msg);
    }
}
