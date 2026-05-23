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
using MiniGamesEmporium.Discord;
using MiniGamesEmporium.Games.DeathrollTournament.Config;

/// <summary>Manages the single Deathroll Tournament Discord webhook: serialises state as POST or PATCH requests with generated images and handles retry and 404-message-gone recovery.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Services;
public sealed class DeathrollDiscordWebhookService : IDisposable
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

    public DeathrollDiscordWebhookService(
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
        var entry = _config.DeathrollTournament.Discord;
        if (!entry.Enabled || string.IsNullOrWhiteSpace(entry.Url)) return;
        if (!forceEvenIfFailed && entry.PostFailed)                  return;

        await _gate.WaitAsync(ct);
        try
        {
            await DispatchAsync(entry, ct);
            _config.Save();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task DispatchAsync(DeathrollTournamentDiscordEntry entry, CancellationToken ct)
    {
        if (!DiscordWebhookUrlParser.TryParseDiscordWebhook(entry.Url, out _, out _)) return;

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
                DiscordWebhookEntryMutations.MarkFailure(entry);
                return;
            }

            if (imageBytes == null)
            {
                DiscordWebhookEntryMutations.MarkFailure(entry);
                return;
            }

            var parts = DiscordWebhookMultipartBuilder.BuildFileParts(imageBytes, imageFileName);

            HttpResponseMessage? response = null;
            try
            {
                response = await DiscordWebhookHttp.SendMultipartWithRetriesAsync(
                    _http, _log, entry, payloadBytes, parts, MaxRetries, ct);

                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
                {
                    if (!isFirstPost && response.StatusCode == HttpStatusCode.NotFound)
                    {
                        entry.MessageId = null;
                        DiscordWebhookEntryMutations.ClearFailure(entry);
                        _config.Save();
                        continue;
                    }
                    DiscordWebhookEntryMutations.MarkFailure(entry);
                    return;
                }

                if (isFirstPost && response.Content != null)
                {
                    var json = await response.Content.ReadAsStringAsync(ct);
                    DiscordWebhookEntryMutations.AssignMessageIdIfPresent(entry, json);
                }

                DiscordWebhookEntryMutations.ClearFailure(entry);
                return;
            }
            catch (HttpRequestException)
            {
                DiscordWebhookEntryMutations.MarkFailure(entry);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unexpected Deathroll Discord error.");
                DiscordWebhookEntryMutations.MarkFailure(entry);
                return;
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    private (byte[]? bytes, string fileName, byte[] payloadJson) BuildPayload(bool isFirstPost)
    {
        var tournament = _config.DeathrollTournamentSession;
        var session    = _config.DeathrollSession;

        if (tournament != null)
        {
            var bracketBytes = Utility.DeathrollDiscordImageRenderer.RenderBracket(tournament);
            var dto          = DeathrollDiscordPayloadFactory.ForActiveTournament(
                tournament, BracketFileName, isFirstPost);
            return (bracketBytes, BracketFileName, Serialize(dto));
        }

        if (session != null)
        {
            var paidPlayers  = GetPaidPlayerNames();
            var playerBytes  = Utility.DeathrollDiscordImageRenderer.RenderPlayerList(
                paidPlayers,
                session.EntryCost,
                session.BoostedPot,
                _config.DeathrollTournament.RegisteredPlayers.Count);
            var dto = DeathrollDiscordPayloadFactory.ForRegistration(
                paidPlayers, session, _config.DeathrollTournament, PlayersFileName, isFirstPost);
            return (playerBytes, PlayersFileName, Serialize(dto));
        }

        var bannerPath = Path.Combine(_imagesDir, BannerFileName);
        if (!File.Exists(bannerPath))
        {
            _log.Error("Deathroll idle banner missing: {Path}", bannerPath);
            return (null, BannerFileName, Array.Empty<byte>());
        }
        var bannerBytes2 = File.ReadAllBytes(bannerPath);
        var idleDto      = DeathrollDiscordPayloadFactory.ForIdle(BannerFileName, isFirstPost);
        return (bannerBytes2, BannerFileName, Serialize(idleDto));
    }

    private List<string> GetPaidPlayerNames()
    {
        var paid       = _config.DeathrollTournament.PaidPlayers;
        var registered = _config.DeathrollTournament.RegisteredPlayers;
        return registered
            .Where(r => paid.Any(p => ParseName(p).Equals(ParseName(r), StringComparison.OrdinalIgnoreCase)))
            .Select(ParseName)
            .ToList();
    }

    private byte[] Serialize<T>(T dto) => JsonSerializer.SerializeToUtf8Bytes(dto, _jsonOpts);

    private static string ParseName(string entry)
    {
        var at = entry.IndexOf('@');
        return at < 0 ? entry.Trim() : entry[..at].Trim();
    }
}
