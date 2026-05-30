using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;
using System.Linq;

/// <summary>Pure helpers for normalising and resolving player name strings across all games.</summary>

namespace MiniGamesEmporium.Utility;
public static class PlayerNameHelper
{
    public static string StripWorld(string name)
    {
        var at = name.IndexOf('@');
        return at >= 0 ? name[..at].Trim() : name.Trim();
    }

    public static string BuildQueueName(SeString sender)
    {
        foreach (var payload in sender.Payloads)
        {
            if (payload is not PlayerPayload pp || string.IsNullOrEmpty(pp.PlayerName))
                continue;

            var world = pp.World.Value.Name.ToString();
            if (!string.IsNullOrEmpty(world))
                return $"{pp.PlayerName}@{world}";

            var playerObj = MiniGamesEmporium.ObjectTable
                .OfType<IPlayerCharacter>()
                .FirstOrDefault(p => p.Name.TextValue.Equals(pp.PlayerName, StringComparison.OrdinalIgnoreCase));
            if (playerObj != null)
            {
                var objWorld = playerObj.HomeWorld.Value.Name.ToString();
                if (!string.IsNullOrEmpty(objWorld))
                    return $"{pp.PlayerName}@{objWorld}";
            }

            return pp.PlayerName;
        }

        return sender.TextValue;
    }
}
