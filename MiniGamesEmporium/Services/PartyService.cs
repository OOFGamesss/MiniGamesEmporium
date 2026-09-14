using System;
using System.Collections.Generic;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.PartyFunctions;
using MiniGamesEmporium.Interop;

/// <summary>Central service that reads local, cross-world and alliance party membership and resolves the matching chat channel for every game.</summary>

namespace MiniGamesEmporium.Services;

public readonly record struct PartyMemberInfo(string Name, string World, ulong ContentId);

public sealed class PartyService
{
    private const long CacheLifetimeMs = 250;

    private readonly List<PartyMemberInfo> members = [];
    private long cacheExpiresAt;

    public bool IsReady => Player.Available;

    public bool IsInAlliance() => UniversalParty.IsAlliance;

    public bool IsInParty() => Svc.Party.Length > 0 || UniversalParty.IsCrossWorldParty;

    public IReadOnlyList<PartyMemberInfo> GetOtherMembers()
    {
        var now = Environment.TickCount64;
        if (now < this.cacheExpiresAt) return this.members;

        this.cacheExpiresAt = now + CacheLifetimeMs;
        this.members.Clear();
        if (!Player.Available) return this.members;

        var localContentId = LocalCharacter.ContentId();
        var source         = UniversalParty.Members;
        for (var i = 0; i < source.Count; i++)
        {
            if (i == 0) continue;
            var member = source[i];
            if (string.IsNullOrEmpty(member.Name)) continue;
            if (localContentId != 0 && member.ContentID == localContentId) continue;
            this.members.Add(new PartyMemberInfo(
                member.Name,
                member.HomeWorld.ValueNullable?.Name.ToString() ?? string.Empty,
                member.ContentID));
        }
        return this.members;
    }

    public void Invalidate() => this.cacheExpiresAt = 0;

    public string? GetPartyChannelToken()
    {
        if (IsInAlliance()) return "/a";
        if (IsInParty())    return "/p";
        return null;
    }

    public string GetDiceCommand(int max)
    {
        if (IsInAlliance()) return $"/dice alliance {max}";
        if (IsInParty())    return $"/dice party {max}";
        return $"/dice {max}";
    }
}
