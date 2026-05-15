using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the win shout to chat when a player rolls the winning number, including the total pot value.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class AnnounceWin
{
    public static void Execute(string playerName, long potAmount, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.WinShoutMessage,
            config,
            playerName,
            totalPotOverride: potAmount);
        chatQueue.Enqueue(msg);
    }
}
