using MiniGamesEmporium.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Utility;

/// <summary>Manages the Deathroll Tournament Discord webhook posting and recovery.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Discord;
public sealed class DeathrollWebhookService : IDisposable
{
    private const int    MaxRetries      = 4;
    private const string BannerFileName  = "deathrolltournamentlogo.png";
    private const string PlayersFileName = "drplayers.png";
    private const string BracketFileName = "drbracket.png";

    private readonly IPluginLog              _log;
    private readonly PluginConfiguration    _config;
    private readonly HttpClient             _http;
    private readonly SemaphoreSlim          _gate = new(1, 1);
    private readonly string                 _imagesDir;
    private readonly CancellationTokenSource _cts = new();

    private readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy    = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingNull,
    };

    public DeathrollWebhookService(
        IPluginLog log,
        PluginConfiguration config,
        string pluginDirectory)
    {
        _log       = log;
        _config    = config;
        _imagesDir = Path.Combine(pluginDirectory, "Images");

        var handler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) };
        _http = new HttpClient(handler, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(90) };
    }

    public void Dispose()
    {
        _cts.Cancel();
        _gate.Dispose();
        _http.Dispose();
        _cts.Dispose();
    }

    public void TriggerSync()
    {
        if (_cts.IsCancellationRequested) return;
        var ct = _cts.Token;
        _ = Task.Run(async () =>
        {
            try   { await SyncCoreAsync(forceEvenIfFailed: false, ct); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _log.Error(ex, "Deathroll Discord background sync failed."); }
        }, ct);
    }

    public async Task ApplyEntryCommittedAsync(CancellationToken ct = default)
    {
        try   { await SyncCoreAsync(forceEvenIfFailed: true, ct); }
        catch (Exception ex) { _log.Error(ex, "Deathroll Discord apply-entry failed."); }
    }

    private async Task SyncCoreAsync(bool forceEvenIfFailed, CancellationToken ct = default)
    {
        var entries = _config.DeathrollTournament.DiscordWebhooks;
        if (entries.Count == 0) return;

        await _gate.WaitAsync(ct);
        try
        {
            var dispatched = false;
            foreach (var entry in entries)
            {
                if (!entry.Enabled || string.IsNullOrWhiteSpace(entry.Url)) continue;
                if (!forceEvenIfFailed && entry.PostFailed) continue;
                await DispatchAsync(entry, ct);
                dispatched = true;
            }
            if (dispatched) _config.Save();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task DispatchAsync(DeathrollTournamentDiscordEntry entry, CancellationToken ct)
    {
        if (!DeathrollWebhookTransport.TryParseUrl(entry.Url, out _, out _)) return;

        while (true)
        {
            var isFirstPost = string.IsNullOrWhiteSpace(entry.MessageId);

            byte[]? imageBytes;
            string  imageFileName;
            byte[]  payloadBytes;

            try
            {
                (imageBytes, imageFileName, payloadBytes) = BuildPayload(isFirstPost);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to build Deathroll Discord payload.");
                entry.PostFailed = true;
                return;
            }

            if (imageBytes == null)
            {
                entry.PostFailed = true;
                return;
            }

            HttpResponseMessage? response = null;
            try
            {
                response = await DeathrollWebhookTransport.SendAsync(
                    _http, _log, entry, payloadBytes, imageBytes, imageFileName, MaxRetries, ct);

                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    if (!isFirstPost && response.StatusCode == HttpStatusCode.NotFound)
                    {
                        entry.MessageId  = null;
                        entry.PostFailed = false;
                        _config.Save();
                        continue;
                    }
                    entry.PostFailed = true;
                    return;
                }

                if (isFirstPost && response.Content != null)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    AssignMessageIdIfPresent(entry, json);
                }

                entry.PostFailed = false;
                return;
            }
            catch (HttpRequestException)
            {
                entry.PostFailed = true;
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unexpected Deathroll Discord error.");
                entry.PostFailed = true;
                return;
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    private static void AssignMessageIdIfPresent(DeathrollTournamentDiscordEntry entry, string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("id", out var idEl)) return;

            var idStr = idEl.GetString();
            if (!string.IsNullOrWhiteSpace(idStr))
                entry.MessageId = idStr.Trim();
        }
        catch (JsonException)
        {
        }
    }

    private (byte[]? bytes, string fileName, byte[] payloadJson) BuildPayload(bool isFirstPost)
    {
        var tournament   = _config.DeathrollTournamentSession;
        var session      = _config.DeathrollSession;
        var username     = _config.DeathrollTournament.WebhookUsername;
        var avatarUrl    = _config.DeathrollTournament.WebhookAvatarUrl;
        var spectatorUrl = GetSpectatorUrl();

        if (tournament != null)
        {
            var totalPot     = DeathrollTournamentService.ComputeTotalPot(_config);
            var bracketBytes = Utility.DeathrollDiscordImageRenderer.RenderBracket(tournament);
            var dto          = DeathrollWebhookContent.ForActiveTournament(
                tournament, BracketFileName, isFirstPost, username, avatarUrl, totalPot, spectatorUrl);
            return (bracketBytes, BracketFileName, Serialize(dto));
        }

        if (session != null)
        {
            var totalPot     = DeathrollTournamentService.ComputeTotalPot(_config);
            var isGilPrize   = DeathrollTournamentService.IsGilPrize(_config);
            var prizeLabel   = DeathrollTournamentService.GetPrizeLabel(_config);
            var paidPlayers  = GetPaidPlayerNames();
            var playerBytes  = Utility.DeathrollDiscordImageRenderer.RenderPlayerList(
                paidPlayers,
                session.EntryCost,
                _config.DeathrollTournament.RegisteredPlayers.Count,
                isGilPrize,
                totalPot,
                prizeLabel);
            var dto = DeathrollWebhookContent.ForRegistration(
                paidPlayers, session, _config.DeathrollTournament, PlayersFileName, isFirstPost, username, avatarUrl, totalPot, spectatorUrl);
            return (playerBytes, PlayersFileName, Serialize(dto));
        }

        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            var idleDto = DeathrollWebhookContent.ForIdle(avatarUrl, isFirstPost, username, avatarUrl, spectatorUrl);
            return (Array.Empty<byte>(), string.Empty, Serialize(idleDto));
        }

        var bannerPath = Path.Combine(_imagesDir, BannerFileName);
        if (!File.Exists(bannerPath))
        {
            _log.Error("Deathroll idle banner missing: {Path}", bannerPath);
            return (null, BannerFileName, Array.Empty<byte>());
        }
        var bannerBytes = File.ReadAllBytes(bannerPath);
        var fallbackDto = DeathrollWebhookContent.ForIdle(
            $"attachment://{BannerFileName}", isFirstPost, username, avatarUrl, spectatorUrl);
        return (bannerBytes, BannerFileName, Serialize(fallbackDto));
    }

    private string? GetSpectatorUrl()
    {
        var drt = _config.DeathrollTournament;
        return drt.WebMirrorEnabled
               && !string.IsNullOrWhiteSpace(drt.WebSessionId)
               && !string.IsNullOrWhiteSpace(drt.WebSpectatorUrl)
            ? drt.WebSpectatorUrl
            : null;
    }

    private List<string> GetPaidPlayerNames()
    {
        var paid       = _config.DeathrollTournament.PaidPlayers;
        var registered = _config.DeathrollTournament.RegisteredPlayers;
        return registered
            .Where(r => paid.Any(p => PlayerInfoService.StripWorld(p).Equals(PlayerInfoService.StripWorld(r), StringComparison.OrdinalIgnoreCase)))
            .Select(PlayerInfoService.StripWorld)
            .ToList();
    }

    private byte[] Serialize<T>(T dto) => JsonSerializer.SerializeToUtf8Bytes(dto, _jsonOpts);

}
