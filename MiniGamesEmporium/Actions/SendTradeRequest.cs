using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Targets a player then sends /trade, queued so the target is set first.</summary>

namespace MiniGamesEmporium.Actions;
public static class SendTradeRequest
{
    public static void Execute(string characterName, ChatQueueService chatQueue)
    {
        chatQueue.EnqueueAction(() =>
        {
            var playerObj = PlayerInfoService.FindNearby(characterName);
            if (playerObj != null)
                MiniGamesEmporium.TargetManager.Target = playerObj;
        });
        chatQueue.Enqueue("/trade");
    }
}
