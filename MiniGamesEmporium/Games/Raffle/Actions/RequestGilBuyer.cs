using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends the Request Gil tell to a player's nominated buyer.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class RequestGilBuyer
{
    public static void Execute(string buyerName, string playerEntry, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = RaffleMessageFormatter.Format(
            config.Raffle.Chat.RequestGilBuyerMessage, config,
            player: PlayerInfoService.StripWorld(playerEntry), buyerName: buyerName.Trim());
        chatQueue.Enqueue(msg);
    }
}
