using MiniGamesEmporium.Services;
using System;
using System.Collections.Generic;
using System.Text;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Utility;

/// <summary>Builds Discord embed payloads for each tournament phase.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Discord;
internal static class DeathrollWebhookContent
{
    private const string FooterText    = "Created by Mini Games Emporium Plogon";
    private const string FooterIconUrl = "https://raw.githubusercontent.com/OOFGamesss/OOFGamesPlugins/main/images/minigamesemporium.png";
    private const int    ColourIdle    = 0x4A0E6E;
    private const int    ColourActive  = unchecked((int)0xB80F5C);
    private const int    ColourWinner  = unchecked((int)0xF0C030);

    private static readonly DiscordFooterDto Footer = new() { Text = FooterText, IconUrl = FooterIconUrl };

    internal static DiscordOutboundPayloadDto ForIdle(string imageUrl, bool applyProfile, string username, string avatarUrl)
    {
        var isAttachment = imageUrl.StartsWith("attachment://", StringComparison.Ordinal);
        return new()
        {
            Username  = applyProfile ? username : null,
            AvatarUrl = applyProfile ? avatarUrl : null,
            Embeds    =
            [
                new DiscordEmbedDto
                {
                    Title  = "No Tournament Active",
                    Color  = ColourIdle,
                    Image  = new DiscordMediaDto(imageUrl),
                    Footer = Footer,
                }
            ],
        Attachments = isAttachment
                ? [new DiscordAttachmentDto { Id = 0, Filename = imageUrl["attachment://".Length..] }]
                : [],
        };
    }

    internal static DiscordOutboundPayloadDto ForRegistration(
        IReadOnlyList<string> paidPlayers,
        DeathrollSessionInfo session,
        DeathrollTournamentConfig cfg,
        string imageFileName,
        bool applyProfile,
        string username,
        string avatarUrl,
        long totalPot)
    {
        var fields = new List<DiscordEmbedFieldDto>
        {
            new() { Name = "Entry Cost", Value = session.EntryCost == 0 ? "Free" : $"{session.EntryCost:N0} Gil", Inline = true },
            new() { Name = "Players",    Value = $"{paidPlayers.Count}",                                           Inline = true },
        };
        if (session.BoostedPot > 0)
            fields.Add(new() { Name = "Boosted Pot", Value = $"{session.BoostedPot:N0} Gil", Inline = true });
        fields.Add(new() { Name = "Current Pot", Value = $"{totalPot:N0} Gil", Inline = false });

        return new()
        {
            Username  = applyProfile ? username : null,
            AvatarUrl = applyProfile ? avatarUrl : null,
            Embeds    =
            [
                new DiscordEmbedDto
                {
                    Title       = "⚔ Tournament Registration",
                    Description = "Players are signing up!",
                    Color       = ColourActive,
                    Image       = new DiscordMediaDto($"attachment://{imageFileName}"),
                    Footer      = Footer,
                    Fields      = fields,
                }
            ],
            Attachments = [new DiscordAttachmentDto { Id = 0, Filename = imageFileName }],
        };
    }

    internal static DiscordOutboundPayloadDto ForActiveTournament(
        DeathrollTournamentState state,
        string imageFileName,
        bool applyProfile,
        string username,
        string avatarUrl,
        long totalPot)
    {
        var hasWinner = state.TournamentWinner != null;
        var colour    = hasWinner ? ColourWinner : ColourActive;
        var title     = hasWinner
            ? $"\U0001F3C6 Tournament Complete - {PlayerInfoService.StripWorld(state.TournamentWinner!)} wins!"
            : "⚔ Deathroll Tournament - In Progress";

        return new()
        {
            Username  = applyProfile ? username : null,
            AvatarUrl = applyProfile ? avatarUrl : null,
            Embeds    =
            [
                new DiscordEmbedDto
                {
                    Title       = title,
                    Description = BuildDescription(state, totalPot),
                    Color       = colour,
                    Image       = new DiscordMediaDto($"attachment://{imageFileName}"),
                    Footer      = Footer,
                }
            ],
            Attachments = [new DiscordAttachmentDto { Id = 0, Filename = imageFileName }],
        };
    }

    private static string BuildDescription(DeathrollTournamentState state, long pot)
    {
        var sb = new StringBuilder();

        if (state.TournamentWinner != null)
        {
            sb.AppendLine($"**Round:** Tournament Complete");
            sb.AppendLine($"**Players:** {state.PlayerCountAtStart}");
            sb.AppendLine($"**Winner:** {PlayerInfoService.StripWorld(state.TournamentWinner)}");
            sb.AppendLine($"**Pot Size:** {pot:N0} Gil");
            return sb.ToString().TrimEnd();
        }

        var r          = state.CurrentRoundIndex;
        var roundCount = state.Rounds.Count;
        var isLast     = r == roundCount - 1;
        var roundLabel = isLast ? "Final" : $"{r + 1}";

        sb.AppendLine($"**Round:** {roundLabel}");
        sb.AppendLine($"**Players:** {state.PlayerCountAtStart}");

        BracketMatch? cur = r < state.Rounds.Count && state.CurrentMatchIndex < state.Rounds[r].Count
            ? state.Rounds[r][state.CurrentMatchIndex] : null;

        if (cur != null)
        {
            var p1    = string.IsNullOrEmpty(cur.Player1) ? "TBD" : PlayerInfoService.StripWorld(cur.Player1);
            var p2    = string.IsNullOrEmpty(cur.Player2) ? "TBD" : PlayerInfoService.StripWorld(cur.Player2);
            var score = state.ActiveMatchPhase != MatchPhase.NotStarted
                ? $" ({state.ActiveMatchPlayer1Wins} - {state.ActiveMatchPlayer2Wins})"
                : string.Empty;
            sb.AppendLine($"**Rolling:** {p1} vs {p2}{score}");
        }

        sb.Append($"**Pot Size:** {pot:N0} Gil");

        return sb.ToString().TrimEnd();
    }

}
