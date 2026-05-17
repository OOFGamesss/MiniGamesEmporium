using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the configured pot announcement to the /yell channel, including the current total pot value.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnouncePot
{
    public static void Execute(PluginConfiguration config, ChatQueueService chatQueue, long? totalPotOverride = null)
    {
        var session = config.ActiveSession;
        var playerName = session?.PlayerName ?? string.Empty;
        var playerWorld = session?.PlayerWorld;
        var fullName = string.IsNullOrEmpty(playerWorld) ? playerName : $"{playerName}@{playerWorld}";
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.YellPotMessage,
            config,
            fullName,
            totalPotOverride: totalPotOverride);
        chatQueue.Enqueue(msg);
    }
}
