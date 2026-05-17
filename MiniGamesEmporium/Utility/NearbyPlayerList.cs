using Dalamud.Game.ClientState.Objects.SubKinds;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Returns a sorted, deduplicated list of all player character names visible in the current area, formatted as Name@HomeWorld.</summary>

namespace MiniGamesEmporium.Utility;
public static class NearbyPlayerList
{
    public static List<string> GetSorted()
    {
        return MiniGamesEmporium.ObjectTable
            .OfType<IPlayerCharacter>()
            .Select(BuildEntry)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildEntry(IPlayerCharacter p)
    {
        var world = p.HomeWorld.Value.Name.ToString();
        return string.IsNullOrEmpty(world) ? p.Name.TextValue : $"{p.Name.TextValue}@{world}";
    }
}
