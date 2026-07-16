using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>Data transfer objects for the Discord webhook API.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Models;

internal sealed class DiscordOutboundPayloadDto
{
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("embeds")]
    public List<DiscordEmbedDto> Embeds { get; init; } = [];

    [JsonPropertyName("attachments")]
    public List<DiscordAttachmentDto>? Attachments { get; init; }

    [JsonPropertyName("components")]
    public List<DiscordComponentRowDto> Components { get; init; } = [];
}

internal sealed class DiscordComponentRowDto
{
    [JsonPropertyName("type")]
    public int Type { get; init; } = 1;

    [JsonPropertyName("components")]
    public List<DiscordLinkButtonDto> Components { get; init; } = [];
}

internal sealed class DiscordLinkButtonDto
{
    [JsonPropertyName("type")]
    public int Type { get; init; } = 2;

    [JsonPropertyName("style")]
    public int Style { get; init; } = 5;

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

internal sealed class DiscordEmbedDto
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("color")]
    public int Color { get; init; }

    [JsonPropertyName("thumbnail")]
    public DiscordMediaDto? Thumbnail { get; init; }

    [JsonPropertyName("image")]
    public DiscordMediaDto? Image { get; init; }

    [JsonPropertyName("fields")]
    public List<DiscordEmbedFieldDto>? Fields { get; init; }

    [JsonPropertyName("footer")]
    public DiscordFooterDto? Footer { get; init; }
}

internal sealed class DiscordEmbedFieldDto
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("inline")]
    public bool Inline { get; init; }
}

internal sealed class DiscordFooterDto
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; init; }
}

internal sealed class DiscordMediaDto
{
    public DiscordMediaDto(string url) => Url = url;

    [JsonPropertyName("url")]
    public string Url { get; }
}

internal sealed class DiscordAttachmentDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("filename")]
    public required string Filename { get; init; }
}
