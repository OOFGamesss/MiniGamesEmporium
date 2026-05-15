using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the unlucky message to chat when a player exhausts all of their rolls without hitting the winning number.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnounceUnlucky
{
    public static void Execute(string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.UnluckyMessage,
            config,
            playerName,
            remainingOverride: "0");
        chatQueue.Enqueue(msg);
    }
}
