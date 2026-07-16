using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

/// <summary>HTTP client for the MiniGames Emporium API.</summary>

namespace MiniGamesEmporium.API;

public sealed class MgeApiClient : IDisposable
{
    private const string BaseUrl = "https://api.oofgames.fyi/v1/minigames-emporium/";

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly HttpClient _http;
    private readonly Func<string> _hostKey;
    private readonly IPluginLog _log;
    private readonly CancellationTokenSource _cts = new();

    public MgeApiClient(Func<string> hostKeyProvider, IPluginLog log)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _hostKey = hostKeyProvider;
        _log = log;
    }

    public Task<(CreateSessionResponse? Data, int Status)> CreateSessionAsync(CreateSessionBody req) =>
        SendAsync<CreateSessionResponse>(HttpMethod.Post, "session", req);

    public Task<(bool Ok, int Status)> SyncStateAsync(string sessionId, object payload) =>
        SendAsync(HttpMethod.Post, $"session/{sessionId}/sync_state", payload);

    public Task<(bool Ok, int Status)> EndSessionAsync(string sessionId) =>
        SendAsync(HttpMethod.Post, $"session/{sessionId}/end", null);

    public Task<(WebJoinRequestsResponse? Data, int Status)> GetJoinRequestsAsync(string sessionId) =>
        SendAsync<WebJoinRequestsResponse>(HttpMethod.Get, $"session/{sessionId}/join_requests", null);

    public Task<(bool Ok, int Status)> AckJoinRequestsAsync(string sessionId, JoinAckBody req) =>
        SendAsync(HttpMethod.Post, $"session/{sessionId}/join_requests/ack", req);

    private static async Task<string> ReadBodySnippetAsync(HttpResponseMessage resp)
    {
        try
        {
            var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Length > 300 ? text[..300] + "..." : text;
        }
        catch { return string.Empty; }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, object? body)
    {
        var uri = new Uri(BaseUrl + path);
        var req = new HttpRequestMessage(method, uri);
        req.Headers.Add("X-Host-Key", _hostKey() ?? string.Empty);
        if (body != null)
            req.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        return req;
    }

    private async Task<(bool Ok, int Status)> SendAsync(HttpMethod method, string path, object? body)
    {
        try
        {
            using var req = BuildRequest(method, path, body);
            using var resp = await _http.SendAsync(req, _cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var detail = await ReadBodySnippetAsync(resp).ConfigureAwait(false);
                _log.Warning($"MGE API {method} {path} -> {(int)resp.StatusCode}{(string.IsNullOrEmpty(detail) ? string.Empty : $": {detail}")}");
            }
            return (resp.IsSuccessStatusCode, (int)resp.StatusCode);
        }
        catch (OperationCanceledException) { _log.Warning($"MGE API {method} {path} timed out or was cancelled."); return (false, 0); }
        catch (Exception ex) { _log.Warning($"MGE API {method} {path} failed: {ex.Message}"); return (false, 0); }
    }

    private async Task<(T? Data, int Status)> SendAsync<T>(HttpMethod method, string path, object? body) where T : class
    {
        try
        {
            using var req = BuildRequest(method, path, body);
            using var resp = await _http.SendAsync(req, _cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                var detail = await ReadBodySnippetAsync(resp).ConfigureAwait(false);
                _log.Warning($"MGE API {method} {path} -> {(int)resp.StatusCode}{(string.IsNullOrEmpty(detail) ? string.Empty : $": {detail}")}");
                return (null, (int)resp.StatusCode);
            }
            var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions, _cts.Token).ConfigureAwait(false);
            return (data, (int)resp.StatusCode);
        }
        catch (OperationCanceledException) { _log.Warning($"MGE API {method} {path} timed out or was cancelled."); return (null, 0); }
        catch (Exception ex) { _log.Warning($"MGE API {method} {path} failed: {ex.Message}"); return (null, 0); }
    }

    public void Dispose()
    {
        try { _cts.Cancel(); } catch { }
        _cts.Dispose();
        _http.Dispose();
    }
}
