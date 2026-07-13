using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Sends a tell to a player listing the raffle ticket numbers they hold.</summary>

namespace MiniGamesEmporium.Games.Raffle.Actions;
public static class TellTicketNumbers
{
    public static void Execute(string playerEntry, string numbers, PluginConfiguration config, ChatQueueService chatQueue)
    {
        var msg = RaffleMessageFormatter.Format(
            config.Raffle.Chat.TicketNumbersMessage, config, player: playerEntry.Trim(), numbers: numbers);
        chatQueue.Enqueue(msg);
    }
}
