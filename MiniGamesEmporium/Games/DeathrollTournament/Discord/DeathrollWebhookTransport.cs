using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Config;

/// <summary>Wire delivery for Deathroll Tournament Discord webhooks: URL parsing, multipart requests, and rate-limit retries.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Discord;

internal static class DeathrollWebhookTransport
{
    internal static async Task<HttpResponseMessage> SendAsync(
        HttpClient http,
        IPluginLog log,
        DeathrollTournamentDiscordEntry entry,
        byte[] payloadJson,
        byte[] fileBytes,
        string fileName,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        if (!TryParseUrl(entry.Url, out var webhookId, out var token))
            throw new HttpRequestException("Webhook URL parsing failed unexpectedly.");

        var createsNewMessage = string.IsNullOrWhiteSpace(entry.MessageId);

        var uri = createsNewMessage
            ? $"https://discord.com/api/webhooks/{webhookId}/{token}?wait=true&with_components=true"
            : $"https://discord.com/api/webhooks/{webhookId}/{token}/messages/{Uri.EscapeDataString(entry.MessageId!.Trim())}?with_components=true";

        HttpResponseMessage? pending = null;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            pending?.Dispose();

            using var multipart = new MultipartFormDataContent();

            var jsonContent = new ByteArrayContent(payloadJson);
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            multipart.Add(jsonContent, "payload_json");

            if (fileBytes.Length > 0)
            {
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                multipart.Add(fileContent, "files[0]", fileName);
            }

            using var request = new HttpRequestMessage(
                createsNewMessage ? HttpMethod.Post : HttpMethod.Patch, uri) { Content = multipart };

            pending = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var status = pending.StatusCode;
            var canRetry = attempt + 1 < maxRetries;

            if (status == HttpStatusCode.TooManyRequests && canRetry)
            {
                var delay = await ResolveDelayAsync(pending, cancellationToken);
                await DrainResponseSafelyAsync(pending, cancellationToken);
                pending.Dispose();
                pending = null;
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            if (!createsNewMessage && status == HttpStatusCode.NotFound)
            {
                await DrainResponseSafelyAsync(pending, cancellationToken);
                var missing = pending;
                pending = null;
                return missing;
            }

            if (!pending.IsSuccessStatusCode && pending.StatusCode != HttpStatusCode.NoContent
                && status >= HttpStatusCode.InternalServerError && canRetry)
            {
                await DrainResponseSafelyAsync(pending, cancellationToken);
                pending.Dispose();
                pending = null;
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                continue;
            }

            if (!pending.IsSuccessStatusCode && pending.StatusCode != HttpStatusCode.NoContent)
            {
                var preview = "(no body)";
                try { preview = ClampOneLine(await pending.Content.ReadAsStringAsync(cancellationToken), 560); }
                catch (Exception bx) { preview = $"(could not read body: {bx.Message})"; }

                log.Warning("Discord webhook HTTP {Verb} rejected: {Status} ({Code}). Snippet={Snippet}",
                    createsNewMessage ? "POST" : "PATCH", status, (int)status, preview);

                var bad = pending;
                pending = null;
                return bad;
            }

            var retained = pending;
            pending = null;
            return retained!;
        }

        pending?.Dispose();
        log.Warning("Discord webhook gave up after {MaxRetries} attempts ({Verb}).",
            maxRetries, createsNewMessage ? "POST" : "PATCH");

        throw new HttpRequestException("Discord webhook retries exhausted.");
    }

    internal static bool TryParseUrl(string raw, out string webhookId, out string token)
    {
        webhookId = string.Empty;
        token = string.Empty;
        var t = raw?.Trim();
        if (string.IsNullOrWhiteSpace(t))
            return false;

        if (!Uri.TryCreate(t, UriKind.Absolute, out var uri))
            return false;

        if (!IsDiscordWebhookHost(uri.Host) || string.IsNullOrEmpty(uri.AbsolutePath))
            return false;

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var idx = Array.IndexOf(segments, "webhooks");
        if (idx < 0 || idx + 2 >= segments.Length)
            return false;

        webhookId = segments[idx + 1];
        var rawToken = segments[idx + 2];
        if (string.IsNullOrWhiteSpace(rawToken))
            return false;

        var tokenPiece = Uri.UnescapeDataString(rawToken.Split('?')[0]);
        token = Uri.UnescapeDataString(tokenPiece.Split('/')[0]).Trim();

        return webhookId.Length > 0 && token.Length > 0;
    }

    private static bool IsDiscordWebhookHost(string host)
    {
        if (string.IsNullOrEmpty(host))
            return false;

        return host.Equals("discord.com", StringComparison.OrdinalIgnoreCase)
               || host.Equals("discordapp.com", StringComparison.OrdinalIgnoreCase)
               || host.Equals("canary.discord.com", StringComparison.OrdinalIgnoreCase)
               || host.Equals("ptb.discord.com", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<TimeSpan> ResolveDelayAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var fromHeader = TryReadRetryAfter(response.Headers);
        if (fromHeader.HasValue)
            return ClampDelay(fromHeader.Value);

        try
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            foreach (var name in new[] { "retry_after", "retryAfter" })
            {
                if (!doc.RootElement.TryGetProperty(name, out var el))
                    continue;

                if (el.ValueKind == JsonValueKind.Number)
                    return ClampDelay(TimeSpan.FromSeconds(Math.Max(0d, el.GetDouble())));

                if (double.TryParse(el.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    return ClampDelay(TimeSpan.FromSeconds(parsed));
            }
        }
        catch
        {
        }

        return TimeSpan.FromSeconds(3);
    }

    private static TimeSpan? TryReadRetryAfter(HttpHeaders headers)
    {
        if (!headers.TryGetValues("retry-after", out var values)) return null;

        foreach (var v in values)
        {
            if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                return TimeSpan.FromSeconds(seconds);

            if (DateTimeOffset.TryParse(v, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var until))
                return until - DateTimeOffset.UtcNow;
        }

        return null;
    }

    private static TimeSpan ClampDelay(TimeSpan raw)
    {
        if (raw <= TimeSpan.Zero) raw = TimeSpan.FromSeconds(1);
        var max = TimeSpan.FromMinutes(10);
        return raw > max ? max : raw;
    }

    private static async Task DrainResponseSafelyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content == null) return;
        try
        {
            await response.Content.CopyToAsync(Stream.Null, cancellationToken);
        }
        catch
        {
        }
    }

    private static string ClampOneLine(string raw, int maxLen)
    {
        if (string.IsNullOrEmpty(raw)) return "(empty)";
        var collapsed = raw.Replace("\r", " ").Replace("\n", " ");
        return collapsed.Length <= maxLen ? collapsed : collapsed[..maxLen] + "…";
    }
}
