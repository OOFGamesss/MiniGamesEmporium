using Dalamud.Game.ClientState.Objects.SubKinds;
using MiniGamesEmporium.Services;
using System;
using System.Linq;

/// <summary>Targets the named player character in the object table and sends a /trade command to initiate a Gil trade with them.</summary>

namespace MiniGamesEmporium.Games.Bar777.Actions;
public static class SendTradeRequest
{
    public static void Execute(string characterName, ChatQueueService chatQueue)
    {
        var playerObj = MiniGamesEmporium.ObjectTable
            .OfType<IPlayerCharacter>()
            .FirstOrDefault(x => x.Name.TextValue.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        if (playerObj != null)
            MiniGamesEmporium.TargetManager.Target = playerObj;
        chatQueue.Enqueue("/trade");
    }
}
