using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.VotingMadness.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;
using System.Linq;

/// <summary>Captures vote keywords from chat while a Voting Madness session is open and listening.</summary>

namespace MiniGamesEmporium.Games.VotingMadness.Events;
public sealed class VotingMadnessKeywordHandler : IChatKeywordHandler
{
    private readonly PluginConfiguration config;
    private readonly VotingMadnessService service;
    private readonly PlayerInfoService playerInfo;

    public VotingMadnessKeywordHandler(PluginConfiguration config, VotingMadnessService service, PlayerInfoService playerInfo)
    {
        this.config     = config;
        this.service    = service;
        this.playerInfo = playerInfo;
    }

    public void TryHandleKeyword(SeString? sender, string message, XivChatType kind)
    {
        var state = this.config.VotingMadnessSession;
        if (state == null || state.IsVotingClosed) return;

        var voter = KeywordMatcher.TryResolveVoter(state.VoteChannels, kind, sender, this.playerInfo);
        if (voter == null) return;

        var keywords = state.Options.Select(o => o.Keyword);
        var matches  = KeywordMatcher.FindWholeWordKeywords(message, keywords);
        if (matches.Count == 0) return;

        this.service.TryCastVotes(voter, matches.Select(m => m.Keyword).ToList());
    }
}
