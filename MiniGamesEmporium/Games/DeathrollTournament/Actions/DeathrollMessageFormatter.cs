using MiniGamesEmporium.Config;

/// <summary>Formats Deathroll Tournament chat message templates by substituting placeholders with live session values.</summary>
/// <remarks>
/// Available placeholders: {player1}, {player2}, {winner}, {totalpot}, {entrycost}, {boostedpot}, {playercount}, {round},
/// {random10}, {firstplayer}, {roundwinner}, {matchwinner}, {matchloser}, {roundscore}, {roundsleft}.
/// </remarks>

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
        var round         = ComputeRoundLabel(tournament);
        return template
            .Replace("{player1}",     StripWorld(player1))
            .Replace("{player2}",     StripWorld(player2))
            .Replace("{winner}",      StripWorld(winner))
            .Replace("{totalpot}",    pot.ToString("N0"))
            .Replace("{entrycost}",   entryCost.ToString("N0"))
            .Replace("{boostedpot}",  boostedPot.ToString("N0"))
            .Replace("{playercount}", playerCount.ToString())
            .Replace("{round}",       round)
            .Replace("{random10}",    random10.ToString())
            .Replace("{firstplayer}", StripWorld(firstPlayer))
            .Replace("{roundwinner}", StripWorld(roundWinner))
            .Replace("{matchwinner}", StripWorld(matchWinner))
            .Replace("{matchloser}",  StripWorld(matchLoser))
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

    private static string ComputeRoundLabel(Games.DeathrollTournament.State.DeathrollTournamentState? tournament)
    {
        if (tournament == null || tournament.Rounds.Count == 0) return string.Empty;
        var idx   = tournament.CurrentRoundIndex;
        var count = tournament.Rounds.Count;
        return idx == count - 1 ? "The Final" : $"Round {idx + 1}";
    }

    private static string StripWorld(string entry)
    {
        var at = entry.IndexOf('@');
        return at >= 0 ? entry[..at].Trim() : entry.Trim();
    }
}
