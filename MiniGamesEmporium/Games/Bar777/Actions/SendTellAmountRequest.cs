using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;

/// <summary>Tells the current player how much Gil they still owe before rolling.</summary>

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
