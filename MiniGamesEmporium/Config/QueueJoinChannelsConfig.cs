using System;

/// <summary>Stores which chat channel types (Say, Shout, Yell, Tell) are monitored for the queue join keyword.</summary>

namespace MiniGamesEmporium.Config;
[Serializable]
public sealed class QueueJoinChannelsConfig
{
    public bool Say { get; set; } = true;
    public bool Shout { get; set; } = true;
    public bool Yell { get; set; } = true;
    public bool TellIncoming { get; set; } = true;
    public bool AnyEnabled() => Say || Shout || Yell || TellIncoming;
}
