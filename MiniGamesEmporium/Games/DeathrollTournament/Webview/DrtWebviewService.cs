using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.API;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Utility;
using PlayerInfo = MiniGamesEmporium.Services.PlayerInfoService;

/// <summary>Mirrors live Deathroll Tournament state to the MiniGames Emporium API and pulls web join requests for host review.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Webview;

public sealed class DrtWebviewService : IDisposable
{
    private const long TickIntervalMs = 250;
    private const long HeartbeatMs = 30_000;
    private const long JoinPollMs = 2_000;

    private static readonly JsonSerializerOptions SigJsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly PluginConfiguration _config;
    private readonly DeathrollTournamentService _deathrollService;
    private readonly IFramework _framework;
    private readonly IPluginLog _log;
    private readonly MgeApiClient _client;

    private readonly List<WebJoinRequestDto> _pendingJoins = new();
    private volatile bool _disposed;
    private bool _busy;
    private bool _authRevoked;
    private bool _hadDeathrollSession;
    private long _lastTick;
    private long _lastHeartbeat;
    private long _lastJoinPoll;
    private long _nextSyncAttempt;
    private int _syncFailures;
    private string _lastSig = string.Empty;

    public string? SessionId { get; private set; }
    public string? SpectatorUrl { get; private set; }
    public string Status { get; private set; } = "Idle";
    public bool Connected { get; private set; }
    public bool StatusIsError { get; private set; }

    public event Action? WebSessionChanged;

    public IReadOnlyList<WebJoinRequestDto> PendingJoins => _pendingJoins;

    public DrtWebviewService(PluginConfiguration config, DeathrollTournamentService deathrollService, MgeApiClient client, IFramework framework, IPluginLog log)
    {
        _config = config;
        _deathrollService = deathrollService;
        _client = client;
        _framework = framework;
        _log = log;

        var drt = config.DeathrollTournament;
        SessionId = string.IsNullOrEmpty(drt.WebSessionId) ? null : drt.WebSessionId;
        SpectatorUrl = string.IsNullOrEmpty(drt.WebSpectatorUrl) ? null : drt.WebSpectatorUrl;
        _hadDeathrollSession = deathrollService.IsSessionActive();
        _deathrollService.SessionUpdated += OnDeathrollSessionUpdated;
        _framework.Update += OnUpdate;
    }

    private void OnDeathrollSessionUpdated()
    {
        var active = _deathrollService.IsSessionActive();
        if (_hadDeathrollSession && !active && !_disposed && SessionId != null)
            EndSession();
        _hadDeathrollSession = active;
    }

    public void GoLive()
    {
        if (_disposed) return;
        if (string.IsNullOrWhiteSpace(_config.Webview.ApiHostKey)) { SetStatus("Enter your API key in Settings first.", true); return; }
        var identity = LocalIdentity();
        if (string.IsNullOrEmpty(identity.Name) || string.IsNullOrEmpty(identity.World))
        {
            SetStatus("Log in to a character before going live.", true);
            return;
        }
        SetStatus("Going live...", false);
        _ = GoLiveAsync(identity);
    }

    public void EndSession()
    {
        var id = SessionId;
        ClearWebSession();
        _authRevoked = false;
        SetStatus("Idle", false);
        if (id != null) _ = _client.EndSessionAsync(id);
    }

    public void Accept(WebJoinRequestDto request)
    {
        _deathrollService.AddPreSignupPlayer(request.CharacterName);
        ResolveJoin(request, "accepted");
    }

    public void Reject(WebJoinRequestDto request)
    {
        ResolveJoin(request, "rejected");
    }

    public bool NameCollides(string name) =>
        _config.DeathrollTournament.RegisteredPlayers
            .Any(p => PlayerInfo.StripWorld(p).Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

    private void ResolveJoin(WebJoinRequestDto request, string status)
    {
        _pendingJoins.RemoveAll(p => p.Id == request.Id);
        var id = SessionId;
        if (id == null) return;
        _ = Task.Run(async () =>
        {
            var (_, ackStatus) = await _client.AckJoinRequestsAsync(id, new JoinAckBody { Ids = new List<long> { request.Id }, Status = status }).ConfigureAwait(false);
            if (ackStatus == 401) HandleAuthRevoked();
        });
    }

    private void ClearWebSession()
    {
        var drt = _config.DeathrollTournament;
        drt.WebMirrorEnabled = false;
        drt.WebSessionId = string.Empty;
        drt.WebSpectatorUrl = string.Empty;
        _config.Save();
        SessionId = null;
        SpectatorUrl = null;
        Connected = false;
        _pendingJoins.Clear();
        WebSessionChanged?.Invoke();
    }

    private void HandleAuthRevoked()
    {
        if (_authRevoked) return;
        _authRevoked = true;
        _log.Warning("MGE API rejected the host key (401) mid-session; stopping the webview locally.");
        ClearWebSession();
        SetStatus("Invalid API key. Get a key from the OOF Games Discord.", true);
    }

    private void HandleSessionGone()
    {
        _log.Warning("MGE API reports the web session no longer exists; stopping the webview locally.");
        ClearWebSession();
        SetStatus("Web session expired. Go live again to restart it.", true);
    }

    private void SetStatus(string text, bool isError)
    {
        Status = text;
        StatusIsError = isError;
    }

    private async Task GoLiveAsync((string ContentId, string Name, string World) identity)
    {
        var venueName = (_config.DeathrollTournament.WebVenueName ?? string.Empty).Trim();
        var venueImage = (_config.DeathrollTournament.WebVenueImageUrl ?? string.Empty).Trim();

        var nameCheck = VenueValidator.ValidateName(venueName);
        if (!nameCheck.Ok) { SetStatus(nameCheck.Error, true); Connected = false; return; }

        var imageCheck = await ValidateVenueImageAsync(venueImage).ConfigureAwait(false);
        if (!imageCheck.Ok) { SetStatus(imageCheck.Error, true); Connected = false; return; }

        var (resp, statusCode) = await _client.CreateSessionAsync(new CreateSessionBody
        {
            HostName = identity.Name,
            VenueName = venueName,
            VenueImageUrl = venueImage,
            CharacterId = identity.ContentId,
            CharacterName = identity.Name,
            World = identity.World,
        }).ConfigureAwait(false);
        if (resp == null || _disposed)
        {
            if (!_disposed)
            {
                Connected = false;
                SetStatus(statusCode switch
                {
                    401 => "Invalid API key. Get a key from the OOF Games Discord.",
                    403 => "This API key has been disabled for key sharing.",
                    404 => "Webview service unavailable. Please try again later.",
                    0 => "Could not reach the server. Check your connection and try again.",
                    _ => $"Could not create session (error {statusCode}).",
                }, true);
            }
            return;
        }

        await _framework.RunOnFrameworkThread(() =>
        {
            SessionId = resp.SessionId;
            SpectatorUrl = resp.SpectatorUrl;
            var drt = _config.DeathrollTournament;
            drt.WebSessionId = resp.SessionId;
            drt.WebSpectatorUrl = resp.SpectatorUrl;
            drt.WebMirrorEnabled = true;
            _config.Save();
            WebSessionChanged?.Invoke();
        }).ConfigureAwait(false);

        _pendingJoins.Clear();
        _lastSig = string.Empty;
        _syncFailures = 0;
        _nextSyncAttempt = 0;
        _authRevoked = false;
        Connected = true;
        SetStatus("Live", false);
    }

    private static async Task<(bool Ok, string Error)> ValidateVenueImageAsync(string url)
    {
        var format = VenueValidator.ValidateImageUrl(url);
        if (!format.Ok) return format;
        if (string.IsNullOrWhiteSpace(url)) return (true, string.Empty);

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return (false, "Could not download the venue image.");
            if (resp.Content.Headers.ContentLength is > 12_000_000) return (false, "Venue image file is too large (max 12MB).");

            var bytes = await resp.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            if (bytes.Length > 12_000_000) return (false, "Venue image file is too large (max 12MB).");
            if (!VenueValidator.TryReadSize(bytes, out var w, out var h)) return (false, "Could not read the venue image dimensions.");
            if (w > VenueValidator.MaxImageDimension || h > VenueValidator.MaxImageDimension)
                return (false, $"Venue image must be no larger than {VenueValidator.MaxImageDimension}x{VenueValidator.MaxImageDimension} pixels.");
            return (true, string.Empty);
        }
        catch (OperationCanceledException) { return (false, "Venue image check was cancelled."); }
        catch (Exception ex) { return (false, $"Venue image check failed: {ex.Message}"); }
    }

    private void OnUpdate(IFramework framework)
    {
        if (_disposed || _busy || _authRevoked) return;
        if (!_config.DeathrollTournament.WebMirrorEnabled || string.IsNullOrWhiteSpace(_config.Webview.ApiHostKey) || SessionId == null) return;

        var now = Environment.TickCount64;
        if (now - _lastTick < TickIntervalMs) return;
        _lastTick = now;

        var payload = DrtWebPayloadBuilder.Build(_config);
        _busy = true;
        _ = TickAsync(payload);
    }

    private async Task TickAsync(DrtWebStateDto payload)
    {
        try
        {
            await PushStateAsync(payload).ConfigureAwait(false);
            if (_authRevoked || SessionId == null) return;

            var now = Environment.TickCount64;
            if (payload.Phase == "registration" && now - _lastJoinPoll > JoinPollMs)
            {
                _lastJoinPoll = now;
                await PollJoinRequestsAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex) { _log.Warning($"Webview tick error: {ex.Message}"); }
        finally { _busy = false; }
    }

    private async Task PushStateAsync(DrtWebStateDto payload)
    {
        var now = Environment.TickCount64;
        if (now < _nextSyncAttempt) return;

        var sig = JsonSerializer.Serialize(payload, SigJsonOptions);
        var attempted = false;
        var ok = true;
        var status = 0;

        if (sig != _lastSig)
        {
            attempted = true;
            (ok, status) = await _client.SyncStateAsync(SessionId!, payload).ConfigureAwait(false);
            if (ok)
            {
                _lastSig = sig;
                _lastHeartbeat = now;
                SetConnected(true);
            }
        }
        else if (now - _lastHeartbeat > HeartbeatMs)
        {
            attempted = true;
            (ok, status) = await _client.SyncStateAsync(SessionId!, payload).ConfigureAwait(false);
            if (ok) { _lastHeartbeat = now; SetConnected(true); }
        }

        if (!attempted) return;
        if (status == 401) { HandleAuthRevoked(); return; }
        if (status == 404 || status == 410) { HandleSessionGone(); return; }
        if (ok)
        {
            _syncFailures = 0;
            _nextSyncAttempt = 0;
        }
        else
        {
            SetConnected(false);
            _syncFailures++;
            _nextSyncAttempt = now + SyncBackoffMs(_syncFailures);
        }
    }

    private static long SyncBackoffMs(int failures)
    {
        var step = Math.Min(Math.Max(failures, 1), 4);
        return 1000L * (1 << (step - 1));
    }

    private async Task PollJoinRequestsAsync()
    {
        var (resp, status) = await _client.GetJoinRequestsAsync(SessionId!).ConfigureAwait(false);
        if (status == 401) { HandleAuthRevoked(); return; }
        if (status == 404 || status == 410) { HandleSessionGone(); return; }
        if (resp == null || _disposed) return;

        await _framework.RunOnFrameworkThread(() =>
        {
            _pendingJoins.Clear();
            _pendingJoins.AddRange(resp.JoinRequests);
        }).ConfigureAwait(false);
    }

    private void SetConnected(bool connected)
    {
        Connected = connected;
        SetStatus(connected ? "Live" : "Reconnecting...", false);
    }

    private static (string ContentId, string Name, string World) LocalIdentity()
    {
        var lp = MiniGamesEmporium.ObjectTable.LocalPlayer;
        var name = lp?.Name.TextValue ?? string.Empty;
        var world = lp?.HomeWorld.Value.Name.ToString() ?? string.Empty;
        var contentId = Interop.LocalCharacter.ContentId().ToString(CultureInfo.InvariantCulture);
        return (contentId, name, world);
    }

    public void Dispose()
    {
        _disposed = true;
        _deathrollService.SessionUpdated -= OnDeathrollSessionUpdated;
        _framework.Update -= OnUpdate;
    }
}
