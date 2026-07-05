using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Tells the current player how much Gil to trade to enter Higher/Lower.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Actions;
public static class RequestEntryFee
{
    public static void Execute(string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = HigherLowerMessageFormatter.Format(
            config.HigherLower.Chat.TellAmountRequestMessage,
            config, playerName: playerName);
        chatQueue.Enqueue(msg);
    }
}
