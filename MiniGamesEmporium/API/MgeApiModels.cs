using System.Collections.Generic;

/// <summary>Request and response models.</summary>

namespace MiniGamesEmporium.API;

public sealed class CreateSessionBody
{
    public string HostName { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string VenueImageUrl { get; set; } = string.Empty;
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string World { get; set; } = string.Empty;
}

public sealed class CreateSessionResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string SpectatorUrl { get; set; } = string.Empty;
}

public sealed class WebJoinRequestDto
{
    public long Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public sealed class WebJoinRequestsResponse
{
    public List<WebJoinRequestDto> JoinRequests { get; set; } = new();
}

public sealed class JoinAckBody
{
    public List<long> Ids { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}
