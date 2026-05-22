using System.Text.Json;
using MiniGamesEmporium.Games.DeathrollTournament.Config;

/// <summary>Mutates a DeathrollTournamentDiscordEntry to record delivery outcomes: assigns the Discord message ID on first post, and marks or clears the failure flag.</summary>

namespace MiniGamesEmporium.Discord;

internal static class DiscordWebhookEntryMutations
{
    internal static void AssignMessageIdIfPresent(DeathrollTournamentDiscordEntry entry, string json)
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

    internal static void MarkFailure(DeathrollTournamentDiscordEntry entry) => entry.PostFailed = true;

    internal static void ClearFailure(DeathrollTournamentDiscordEntry entry) => entry.PostFailed = false;
}
