using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiniGamesEmporium.Discord;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;

/// <summary>Builds Discord embed payloads for each tournament phase: idle, registration, active bracket, and tournament complete.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Services;
internal static class DeathrollDiscordPayloadFactory
{
    private const string WebhookUsername  = "Deathroll Tournament";
    private const string AvatarUrl        = "https://raw.githubusercontent.com/OOFGamesss/OOFGamesPlugins/main/images/deathrolltournament.png";
    private const string FooterText       = "Created by Mini Games Emporium Plogon";
    private const string FooterIconUrl    = "https://raw.githubusercontent.com/OOFGamesss/OOFGamesPlugins/main/images/minigamesemporium.png";
    private const int    ColourIdle       = 0x4A0E6E;
    private const int    ColourActive     = unchecked((int)0xB80F5C);
    private const int    ColourWinner     = unchecked((int)0xF0C030);

    private static readonly DiscordFooterDto Footer = new() { Text = FooterText, IconUrl = FooterIconUrl };

    internal static DiscordOutboundPayloadDto ForIdle(string bannerFileName, bool applyProfile) =>
        new()
        {
            Username  = applyProfile ? WebhookUsername : null,
            AvatarUrl = applyProfile ? AvatarUrl : null,
            Embeds    =
            [
                new DiscordEmbedDto
                {
                    Title  = "No Tournament Active",
                    Color  = ColourIdle,
                    Image  = new DiscordMediaDto($"attachment://{bannerFileName}"),
                    Footer = Footer,
                }
            ],
            Attachments = [new DiscordAttachmentDto { Id = 0, Filename = bannerFileName }],
        };

    internal static DiscordOutboundPayloadDto ForRegistration(
        IReadOnlyList<string> paidPlayers,
        DeathrollSessionInfo session,
        DeathrollTournamentConfig cfg,
        string imageFileName,
        bool applyProfile)
    {
        var pot    = session.EntryCost * paidPlayers.Count + session.BoostedPot;
        var fields = new List<DiscordEmbedFieldDto>
        {
            new() { Name = "Entry Cost",  Value = session.EntryCost == 0 ? "Free" : $"{session.EntryCost:N0} Gil", Inline = true },
            new() { Name = "Players",     Value = $"{paidPlayers.Count}",                                       Inline = true },
            new() { Name = "Current Pot", Value = $"{pot:N0} Gil",                               Inline = true },
        };
        if (session.BoostedPot > 0)
            fields.Add(new() { Name = "Boosted Pot", Value = $"{session.BoostedPot:N0} Gil", Inline = true });

        return new()
        {
            Username  = applyProfile ? WebhookUsername : null,
            AvatarUrl = applyProfile ? AvatarUrl : null,
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
        bool applyProfile)
    {
        var hasWinner = state.TournamentWinner != null;
        var colour    = hasWinner ? ColourWinner : ColourActive;
        var title     = hasWinner
            ? $"\U0001F3C6 Tournament Complete - {ParseName(state.TournamentWinner!)} wins!"
            : "⚔ Deathroll Tournament - In Progress";

        return new()
        {
            Username  = applyProfile ? WebhookUsername : null,
            AvatarUrl = applyProfile ? AvatarUrl : null,
            Embeds    =
            [
                new DiscordEmbedDto
                {
                    Title       = title,
                    Description = BuildDescription(state),
                    Color       = colour,
                    Image       = new DiscordMediaDto($"attachment://{imageFileName}"),
                    Footer      = Footer,
                }
            ],
            Attachments = [new DiscordAttachmentDto { Id = 0, Filename = imageFileName }],
        };
    }

    private static string BuildDescription(DeathrollTournamentState state)
    {
        var sb  = new StringBuilder();
        var pot = state.EntryCostAtStart * state.PlayerCountAtStart + state.BoostedPotAtStart;

        if (state.TournamentWinner != null)
        {
            sb.AppendLine($"**Round:** Tournament Complete");
            sb.AppendLine($"**Players:** {state.PlayerCountAtStart}");
            sb.AppendLine($"**Winner:** {ParseName(state.TournamentWinner)}");
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
            var p1    = string.IsNullOrEmpty(cur.Player1) ? "TBD" : ParseName(cur.Player1);
            var p2    = string.IsNullOrEmpty(cur.Player2) ? "TBD" : ParseName(cur.Player2);
            var score = state.ActiveMatchPhase != MatchPhase.NotStarted
                ? $" ({state.ActiveMatchPlayer1Wins} - {state.ActiveMatchPlayer2Wins})"
                : string.Empty;
            sb.AppendLine($"**Rolling:** {p1} vs {p2}{score}");
        }

        sb.Append($"**Pot Size:** {pot:N0} Gil");

        return sb.ToString().TrimEnd();
    }

    private static string ParseName(string entry)
    {
        var at = entry.IndexOf('@');
        return at < 0 ? entry.Trim() : entry[..at].Trim();
    }
}
