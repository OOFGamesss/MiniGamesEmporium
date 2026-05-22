using Dalamud.Game.ClientState.Objects.SubKinds;
using MiniGamesEmporium.Services;
using System;
using System.Linq;

/// <summary>Enqueues a target-and-trade sequence so the player is targeted immediately before /trade fires, preventing a stale target if other messages are already queued.</summary>

namespace MiniGamesEmporium.Actions;
public static class SendTradeRequest
{
    public static void Execute(string characterName, ChatQueueService chatQueue)
    {
        chatQueue.EnqueueAction(() =>
        {
            var playerObj = MiniGamesEmporium.ObjectTable
                .OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.TextValue.Equals(characterName, StringComparison.OrdinalIgnoreCase));
            if (playerObj != null)
                MiniGamesEmporium.TargetManager.Target = playerObj;
        });
        chatQueue.Enqueue("/trade");
    }
}
