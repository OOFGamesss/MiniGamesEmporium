using MiniGamesEmporium.Config;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

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
        int    roundsLeft  = 0)
    {
        var tournament    = config.DeathrollTournamentSession;
        var activeSession = config.DeathrollSession;
        var cfg           = config.DeathrollTournament;
        var pot           = totalPotOverride ?? ComputeTotalPot(config);
        var entryCost     = tournament?.EntryCostAtStart  ?? activeSession?.EntryCost  ?? cfg.EntryCost;
        var boostedPot    = tournament?.BoostedPotAtStart ?? activeSession?.BoostedPot ?? cfg.BoostedPot;
        var playerCount   = tournament?.PlayerCountAtStart ?? cfg.RegisteredPlayers.Count;
        var round         = tournament == null || tournament.Rounds.Count == 0
            ? string.Empty
            : tournament.CurrentRoundLabel();
        return template
            .Replace("{player1}",     PlayerInfoService.StripWorld(player1))
            .Replace("{player2}",     PlayerInfoService.StripWorld(player2))
            .Replace("{winner}",      PlayerInfoService.StripWorld(winner))
            .Replace("{totalpot}",    pot.ToString("N0"))
            .Replace("{entrycost}",   entryCost.ToString("N0"))
            .Replace("{boostedpot}",  boostedPot.ToString("N0"))
            .Replace("{playercount}", playerCount.ToString())
            .Replace("{round}",       round)
            .Replace("{random10}",    random10.ToString())
            .Replace("{firstplayer}", PlayerInfoService.StripWorld(firstPlayer))
            .Replace("{roundwinner}", PlayerInfoService.StripWorld(roundWinner))
            .Replace("{matchwinner}", PlayerInfoService.StripWorld(matchWinner))
            .Replace("{matchloser}",  PlayerInfoService.StripWorld(matchLoser))
            .Replace("{roundscore}",  roundScore)
            .Replace("{roundsleft}",  roundsLeft.ToString());
    }

    public static long ComputeTotalPot(PluginConfiguration config)
    {
        var tournament = config.DeathrollTournamentSession;
        if (tournament != null)
            return tournament.EntryCostAtStart * tournament.PlayerCountAtStart + tournament.BoostedPotAtStart;
        var activeSession = config.DeathrollSession;
        var cfg = config.DeathrollTournament;
        var entryCost  = activeSession?.EntryCost  ?? cfg.EntryCost;
        var boostedPot = activeSession?.BoostedPot ?? cfg.BoostedPot;
        return entryCost * cfg.RegisteredPlayers.Count + boostedPot;
    }

}
