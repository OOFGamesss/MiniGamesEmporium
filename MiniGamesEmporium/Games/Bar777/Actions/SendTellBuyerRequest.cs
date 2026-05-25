using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends a /tell to the buyer informing them of the Gil cost and which player they are paying for.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class SendTellBuyerRequest
{
    public static void Execute(string buyerName, string playerName, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var at = playerName.IndexOf('@');
        var strippedPlayer = at >= 0 ? playerName[..at].Trim() : playerName.Trim();
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.TellBuyerRequestMessage,
            config,
            strippedPlayer,
            buyerName: buyerName);
        chatQueue.Enqueue(msg);
    }
}
