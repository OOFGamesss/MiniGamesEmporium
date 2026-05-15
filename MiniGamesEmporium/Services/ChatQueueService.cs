using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using System;

/// <summary>Wraps the ECommons TaskManager to dispatch chat commands sequentially, inserting a one-second delay between each message to avoid flooding the chat server.</summary>

namespace MiniGamesEmporium.Services;
public sealed class ChatQueueService : IDisposable
{
    private readonly TaskManager taskManager = new();
    public void Enqueue(string message)
    {
        taskManager.Enqueue(() => Chat.SendMessage(message));
        taskManager.EnqueueDelay(1000);
    }
    public void Dispose() => taskManager.Dispose();
}
