using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

/// <summary>Constructs MultipartFormDataContent for Discord webhook POST and PATCH requests, attaching the JSON payload and PNG image file parts.</summary>

namespace MiniGamesEmporium.Discord;

internal static class DiscordWebhookMultipartBuilder
{
    internal static IReadOnlyList<DiscordMultipartFilePart> BuildFileParts(
        byte[] fileBytes,
        string fileName) =>
        new List<DiscordMultipartFilePart> { new(fileBytes, fileName, 0) };

    internal static MultipartFormDataContent BuildContent(
        byte[] payloadJsonUtf8,
        IReadOnlyList<DiscordMultipartFilePart> files)
    {
        var multipart = new MultipartFormDataContent();
        var jsonContent = new ByteArrayContent(payloadJsonUtf8);
        jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        multipart.Add(jsonContent, "payload_json");

        foreach (var part in files.OrderBy(static f => f.Slot))
        {
            var fileContent = new ByteArrayContent(part.Data);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            multipart.Add(fileContent, $"files[{part.Slot}]", part.Filename);
        }

        return multipart;
    }
}
