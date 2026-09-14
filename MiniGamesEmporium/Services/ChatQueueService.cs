using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using MiniGamesEmporium.Utility;
using System;

/// <summary>Dispatches chat commands and actions sequentially, spacing messages to avoid flooding.</summary>

namespace MiniGamesEmporium.Services;
public sealed class ChatQueueService : IDisposable
{
    private readonly TaskManager taskManager = new();
    private readonly PartyService partyService;

    public ChatQueueService(PartyService partyService)
    {
        this.partyService = partyService;
    }

    public void Enqueue(string message, int delayMs = 1000)
    {
        taskManager.Enqueue(() => Chat.SendMessage(message));
        taskManager.EnqueueDelay(delayMs);
    }

    public void EnqueueToParty(string message, int delayMs = 1000) =>
        Enqueue(ChatChannelText.ApplyPartyChannel(message, this.partyService.GetPartyChannelToken()), delayMs);

    public void EnqueueAction(Action action) => taskManager.Enqueue(action);
    public void Dispose() => taskManager.Dispose();
}
