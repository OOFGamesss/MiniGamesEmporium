using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;
using System;

/// <summary>Sends a /tell to the current session player informing them of the Gil amount they still owe before their rolls can begin.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class SendTellAmountRequest
{
    public static void Execute(string playerName, PluginConfiguration config, ChatQueueService chatQueue, int amountPaid = 0)
    {
        var gilOwed = Math.Max(0, config.Bar777.CostPerRoll - amountPaid);
        var msg = Bar777MessageFormatter.Format(
            config.Bar777.Chat.TellAmountRequestMessage,
            config,
            playerName,
            remainingOverride: gilOwed.ToString("N0"));
        chatQueue.Enqueue(msg);
    }
}
