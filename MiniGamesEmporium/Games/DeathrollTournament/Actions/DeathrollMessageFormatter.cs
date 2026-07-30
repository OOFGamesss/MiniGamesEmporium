using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;
using System.Collections.Generic;
using System.Linq;

/// <summary>Fills Deathroll Tournament message templates with live session values.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Actions;
public static class DeathrollMessageFormatter
{
    public static string Format(
        string template,
        PluginConfiguration config,
        string player1 = "",
        string player2 = "",
        string winner = "",
        long? totalPotOverride = null,
        int random10 = 0,
        string firstPlayer = "",
        string roundWinner = "",
        string matchWinner = "",
        string matchLoser  = "",
        string roundScore  = "",
        int    roundsLeft  = 0,
        string betTarget   = "",
        long   bettingPotOverride = 0,
        IReadOnlyList<string>? betWinners = null)
    {
        var tournament    = config.DeathrollTournamentSession;
        var activeSession = config.DeathrollSession;
        var cfg           = config.DeathrollTournament;
        var entryCost     = tournament?.EntryCostAtStart  ?? activeSession?.EntryCost  ?? cfg.EntryCost;
        var boostedPot    = tournament?.BoostedPotAtStart ?? activeSession?.BoostedPot ?? cfg.BoostedPot;
        var betUnit       = tournament?.BetUnitAtStart    ?? activeSession?.BetUnit    ?? cfg.BetUnit;
        var playerCount   = tournament?.PlayerCountAtStart ?? cfg.RegisteredPlayers.Count;
        var round         = tournament == null || tournament.Rounds.Count == 0
            ? string.Empty
            : tournament.CurrentRoundLabel();
        var betWinnersList = betWinners == null
            ? string.Empty
            : string.Join(", ", betWinners.Select(PlayerInfoService.StripWorld));
        var prizeLabel = DeathrollTournamentService.GetPrizeLabel(config, totalPotOverride);
        if (string.IsNullOrWhiteSpace(prizeLabel)) prizeLabel = "the prize";
        return template
            .Replace("{player1}",       PlayerInfoService.StripWorld(player1))
            .Replace("{player2}",       PlayerInfoService.StripWorld(player2))
            .Replace("{winner}",        PlayerInfoService.StripWorld(winner))
            .Replace("{prize}",         prizeLabel)
            .Replace("{entrycost}",     entryCost.ToString("N0"))
            .Replace("{boostedpot}",    boostedPot.ToString("N0"))
            .Replace("{playercount}",   playerCount.ToString())
            .Replace("{round}",         round)
            .Replace("{random10}",      random10.ToString())
            .Replace("{firstplayer}",   PlayerInfoService.StripWorld(firstPlayer))
            .Replace("{roundwinner}",   PlayerInfoService.StripWorld(roundWinner))
            .Replace("{matchwinner}",   PlayerInfoService.StripWorld(matchWinner))
            .Replace("{matchloser}",    PlayerInfoService.StripWorld(matchLoser))
            .Replace("{roundscore}",    roundScore)
            .Replace("{roundsleft}",    roundsLeft.ToString())
            .Replace("{betunit}",       betUnit.ToString("N0"))
            .Replace("{betkeyword}",    cfg.BetKeyword)
            .Replace("{bettarget}",     PlayerInfoService.StripWorld(betTarget))
            .Replace("{bettingpot}",    bettingPotOverride.ToString("N0"))
            .Replace("{betwinners}",    betWinnersList)
            .Replace("{url}",           cfg.WebSpectatorUrl ?? string.Empty);
    }
}
