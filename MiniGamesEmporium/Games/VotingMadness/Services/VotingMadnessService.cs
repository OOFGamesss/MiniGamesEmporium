using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Models;
using MiniGamesEmporium.Games.VotingMadness.State;
using MiniGamesEmporium.Games.VotingMadness.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Manages Voting Madness session lifecycle, vote tallies and winning option results.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Services;
public sealed class VotingMadnessService
{
    private static readonly Vector4[] Palette =
    [
        new(0.72f, 1.00f, 0.08f, 1f),
        new(0.20f, 0.75f, 1.00f, 1f),
        new(1.00f, 0.45f, 0.20f, 1f),
        new(0.85f, 0.35f, 0.95f, 1f),
        new(1.00f, 0.85f, 0.20f, 1f),
        new(0.30f, 0.95f, 0.55f, 1f),
        new(1.00f, 0.40f, 0.65f, 1f),
        new(0.45f, 0.55f, 1.00f, 1f),
    ];

    private readonly PluginConfiguration config;
    private readonly HistoryService historyService;
    private readonly Random rng = new();

    public event Action? SessionUpdated;

    public VotingMadnessService(PluginConfiguration config, HistoryService historyService)
    {
        this.config         = config;
        this.historyService = historyService;
    }

    public bool IsSessionActive() => this.config.VotingMadnessSession != null;
    public VotingMadnessState? GetState() => this.config.VotingMadnessSession;

    public void StartSession()
    {
        if (this.config.VotingMadnessSession != null) return;
        var cfg = this.config.VotingMadness;
        var options = cfg.Options
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (options.Count < 2) return;

        var state = new VotingMadnessState
        {
            StartedAt          = DateTime.UtcNow,
            MultipleChoice     = cfg.MultipleChoice,
            AllowMultipleVotes = cfg.AllowMultipleVotes,
            VoteChannels       = CloneChannels(cfg.VoteChannels),
            CloseHour          = cfg.CloseHour,
            CloseMinute        = cfg.CloseMinute,
            Options            = BuildOptions(options),
        };
        if (state.CloseHour >= 0)
            state.CloseAtUtc = ServerTimeUtil.NextOccurrence(state.CloseHour, state.CloseMinute);

        this.config.VotingMadnessSession = state;
        Save();
        SessionUpdated?.Invoke();
    }

    public void StopSession()
    {
        RecordSessionHistory();
        this.config.VotingMadnessSession = null;
        Save();
        SessionUpdated?.Invoke();
    }

    public void StopVote()
    {
        var state = this.config.VotingMadnessSession;
        if (state == null || state.IsVotingClosed) return;
        state.IsVotingClosed = true;
        Save();
        SessionUpdated?.Invoke();
    }

    public bool TryCastVotes(string playerEntry, IReadOnlyList<string> matchedKeywords)
    {
        var state = this.config.VotingMadnessSession;
        if (state == null || state.IsVotingClosed) return false;
        if (string.IsNullOrWhiteSpace(playerEntry) || matchedKeywords.Count == 0) return false;

        var player = playerEntry.Trim();
        if (!state.MultipleChoice && !state.AllowMultipleVotes && HasAnyVote(state, player))
            return false;

        var keywords = state.MultipleChoice
            ? matchedKeywords.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : [matchedKeywords[0]];

        var added = false;
        foreach (var keyword in keywords)
        {
            var option = ResolveOption(state, keyword);
            if (option == null) continue;
            if (!state.AllowMultipleVotes && HasVoteForOption(state, player, option.Keyword))
                continue;

            state.Votes.Add(new VoteRecord
            {
                PlayerName     = player,
                OptionKeyword  = option.Keyword,
                CastAtUtc      = DateTime.UtcNow,
            });
            added = true;
        }

        if (!added) return false;

        Save();
        SessionUpdated?.Invoke();
        return true;
    }

    public void ClearPlayerVotes(string playerEntry)
    {
        var state = this.config.VotingMadnessSession;
        if (state == null || string.IsNullOrWhiteSpace(playerEntry)) return;
        var name = PlayerInfoService.StripWorld(playerEntry);
        var removed = state.Votes.RemoveAll(v =>
            PlayerInfoService.StripWorld(v.PlayerName).Equals(name, StringComparison.OrdinalIgnoreCase));
        if (removed == 0) return;
        Save();
        SessionUpdated?.Invoke();
    }

    public int CountVotesFor(string optionKeyword)
    {
        var state = this.config.VotingMadnessSession;
        if (state == null) return 0;
        return state.Votes.Count(v =>
            v.OptionKeyword.Equals(optionKeyword, StringComparison.OrdinalIgnoreCase));
    }

    public int ComputeTotalVotes() => this.config.VotingMadnessSession?.Votes.Count ?? 0;

    public int ComputeUniqueVoters()
    {
        var state = this.config.VotingMadnessSession;
        if (state == null) return 0;
        return state.Votes
            .Select(v => PlayerInfoService.StripWorld(v.PlayerName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    public IReadOnlyList<string> GetLeadingOptions()
    {
        var state = this.config.VotingMadnessSession;
        if (state == null || state.Options.Count == 0) return [];
        var counts = state.Options
            .Select(o => (o.Keyword, Count: CountVotesFor(o.Keyword)))
            .ToList();
        var max = counts.Max(c => c.Count);
        if (max <= 0) return [];
        return counts.Where(c => c.Count == max).Select(c => c.Keyword).ToList();
    }

    public (IReadOnlyList<string> Winners, int Votes, float Percent, bool IsTie) GetResult()
    {
        var leaders = GetLeadingOptions();
        var total   = ComputeTotalVotes();
        if (leaders.Count == 0 || total == 0)
            return ([], 0, 0f, false);
        var votes = CountVotesFor(leaders[0]);
        var percent = total == 0 ? 0f : votes * 100f / total;
        return (leaders, votes, percent, leaders.Count > 1);
    }

    public IReadOnlyList<(string PlayerName, string World, string VotesLabel)> GetPlayerRows()
    {
        var state = this.config.VotingMadnessSession;
        if (state == null) return [];

        return state.Votes
            .GroupBy(v => PlayerInfoService.StripWorld(v.PlayerName), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var full = g.First().PlayerName;
                var (name, world) = PlayerInfoService.SplitNameAndWorld(full);
                var label = string.Join(", ",
                    g.GroupBy(v => v.OptionKeyword, StringComparer.OrdinalIgnoreCase)
                        .Select(og =>
                        {
                            var count = og.Count();
                            return count == 1 ? og.Key : $"{og.Key} x{count}";
                        }));
                return (name, world, label);
            })
            .OrderBy(r => r.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string FormatStandings()
    {
        var state = this.config.VotingMadnessSession;
        if (state == null) return string.Empty;
        var total = ComputeTotalVotes();
        return string.Join(", ",
            state.Options.Select(o =>
            {
                var count = CountVotesFor(o.Keyword);
                var pct   = total == 0 ? 0 : (int)Math.Round(count * 100.0 / total);
                return $"{o.Keyword} {count} ({pct}%)";
            }));
    }

    public string FormatOptionsList()
    {
        var state = this.config.VotingMadnessSession;
        if (state != null && state.Options.Count > 0)
            return string.Join(", ", state.Options.Select(o => o.Keyword));
        return string.Join(", ",
            this.config.VotingMadness.Options.Where(o => !string.IsNullOrWhiteSpace(o)));
    }

    private void RecordSessionHistory()
    {
        var state = this.config.VotingMadnessSession;
        if (state == null) return;
        var (winners, _, _, isTie) = GetResult();
        var winnerLabel = winners.Count == 0
            ? string.Empty
            : isTie ? $"Tie: {string.Join(", ", winners)}" : winners[0];
        this.historyService.AddSession(new SessionRecord
        {
            GameName      = VotingMadnessGameIds.DisplayName,
            Winner        = winnerLabel,
            PlayersPlayed = ComputeUniqueVoters(),
            RoundsPlayed  = ComputeTotalVotes(),
            Timestamp     = DateTime.UtcNow,
        });
    }

    private List<VotingOption> BuildOptions(List<string> keywords)
    {
        var shuffled = Palette.OrderBy(_ => this.rng.Next()).ToArray();
        var list = new List<VotingOption>(keywords.Count);
        for (var i = 0; i < keywords.Count; i++)
        {
            var colour = shuffled[i % shuffled.Length];
            if (i >= shuffled.Length)
            {
                colour = new Vector4(
                    0.25f + (float)this.rng.NextDouble() * 0.7f,
                    0.25f + (float)this.rng.NextDouble() * 0.7f,
                    0.25f + (float)this.rng.NextDouble() * 0.7f,
                    1f);
            }
            list.Add(new VotingOption
            {
                Keyword = keywords[i],
                ColourR = colour.X,
                ColourG = colour.Y,
                ColourB = colour.Z,
                ColourA = colour.W,
            });
        }
        return list;
    }

    private static bool HasAnyVote(VotingMadnessState state, string playerEntry)
    {
        var name = PlayerInfoService.StripWorld(playerEntry);
        return state.Votes.Any(v =>
            PlayerInfoService.StripWorld(v.PlayerName).Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasVoteForOption(VotingMadnessState state, string playerEntry, string optionKeyword)
    {
        var name = PlayerInfoService.StripWorld(playerEntry);
        return state.Votes.Any(v =>
            PlayerInfoService.StripWorld(v.PlayerName).Equals(name, StringComparison.OrdinalIgnoreCase)
            && v.OptionKeyword.Equals(optionKeyword, StringComparison.OrdinalIgnoreCase));
    }

    private static VotingOption? ResolveOption(VotingMadnessState state, string keyword) =>
        state.Options.FirstOrDefault(o =>
            o.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));

    private static QueueConfig CloneChannels(QueueConfig src) => new()
    {
        Say          = src.Say,
        Shout        = src.Shout,
        Yell         = src.Yell,
        TellIncoming = src.TellIncoming,
    };

    private void Save() => this.config.Save();
}
