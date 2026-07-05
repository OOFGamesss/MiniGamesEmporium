using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>Central service for host player details and player name resolution across all games.</summary>

namespace MiniGamesEmporium.Services;
public sealed class PlayerInfoService
{
    public string HostName =>
        MiniGamesEmporium.ObjectTable.LocalPlayer?.Name.TextValue ?? string.Empty;

    public string HostWorld =>
        MiniGamesEmporium.ObjectTable.LocalPlayer?.HomeWorld.Value.Name.ToString() ?? string.Empty;

    public string HostFullName
    {
        get
        {
            var name = HostName;
            if (string.IsNullOrEmpty(name)) return string.Empty;
            var world = HostWorld;
            return string.IsNullOrEmpty(world) ? name : $"{name}@{world}";
        }
    }

    public bool IsHost(string name)
    {
        var host = HostName;
        return !string.IsNullOrEmpty(host)
            && StripWorld(name).Equals(host, StringComparison.OrdinalIgnoreCase);
    }

    public static string StripWorld(string name)
    {
        var at = name.IndexOf('@');
        return at >= 0 ? name[..at].Trim() : name.Trim();
    }

    public static (string Name, string World) SplitNameAndWorld(string raw)
    {
        var at = raw.IndexOf('@');
        return at < 0 ? (raw.Trim(), string.Empty) : (raw[..at].Trim(), raw[(at + 1)..].Trim());
    }

    public static IPlayerCharacter? FindNearby(string name) =>
        MiniGamesEmporium.ObjectTable
            .OfType<IPlayerCharacter>()
            .FirstOrDefault(p => p.Name.TextValue.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static string BuildQueueName(SeString sender)
    {
        foreach (var payload in sender.Payloads)
        {
            if (payload is not PlayerPayload pp || string.IsNullOrEmpty(pp.PlayerName))
                continue;

            var world = pp.World.Value.Name.ToString();
            if (!string.IsNullOrEmpty(world))
                return $"{pp.PlayerName}@{world}";

            var playerObj = FindNearby(pp.PlayerName);
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

    public static List<string> GetNearbySorted() =>
        MiniGamesEmporium.ObjectTable
            .OfType<IPlayerCharacter>()
            .Select(NameWithWorld)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string NameWithWorld(IPlayerCharacter p)
    {
        var world = p.HomeWorld.Value.Name.ToString();
        return string.IsNullOrEmpty(world) ? p.Name.TextValue : $"{p.Name.TextValue}@{world}";
    }
}
