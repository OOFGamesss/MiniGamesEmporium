using System;
using Dalamud.Game.Text;

/// <summary>Stores which chat channels are watched for the queue join keyword.</summary>

namespace MiniGamesEmporium.Config;
[Serializable]
public sealed class QueueConfig
{
    public bool Say { get; set; } = true;
    public bool Shout { get; set; } = true;
    public bool Yell { get; set; } = true;
    public bool TellIncoming { get; set; } = true;
    public bool AnyEnabled() => Say || Shout || Yell || TellIncoming;

    public bool Matches(XivChatType kind) => kind switch
    {
        XivChatType.Say          => Say,
        XivChatType.Shout        => Shout,
        XivChatType.Yell         => Yell,
        XivChatType.TellIncoming => TellIncoming,
        _                        => false,
    };
}
