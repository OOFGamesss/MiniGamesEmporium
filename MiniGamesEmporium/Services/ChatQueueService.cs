using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using System;

/// <summary>Dispatches chat commands and actions sequentially, spacing messages to avoid flooding.</summary>

namespace MiniGamesEmporium.Services;
public sealed class ChatQueueService : IDisposable
{
    private readonly TaskManager taskManager = new();
    public void Enqueue(string message)
    {
        taskManager.Enqueue(() => Chat.SendMessage(message));
        taskManager.EnqueueDelay(1000);
    }
    public void EnqueueAction(Action action) => taskManager.Enqueue(action);
    public void Dispose() => taskManager.Dispose();
}
