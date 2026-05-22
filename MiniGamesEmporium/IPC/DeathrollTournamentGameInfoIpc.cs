using System;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;

/// <summary>Serialisable snapshot of the current Deathroll Tournament session state, exposed via the IPC gate for consumption by external plugins.</summary>

namespace MiniGamesEmporium.IPC;

[Serializable]
public sealed class DeathrollTournamentGameInfoIpc
{
    public string GameLabel { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public long BoostedPot { get; set; }
    public long TotalPot { get; set; }
    public long EntryCost { get; set; }
    public int PlayersEntered { get; set; }

    internal static DeathrollTournamentGameInfoIpc? FromConfig(PluginConfiguration config)
    {
        if (config.DeathrollSession == null) return null;

        var session    = config.DeathrollSession;
        var tournament = config.DeathrollTournamentSession;
        var cfg        = config.DeathrollTournament;

        var players    = tournament?.PlayerCountAtStart ?? cfg.PaidPlayers.Count;
        var boostedPot = tournament?.BoostedPotAtStart  ?? session.BoostedPot;
        var entryCost  = tournament?.EntryCostAtStart   ?? session.EntryCost;
        var totalPot   = tournament != null
            ? tournament.EntryCostAtStart * tournament.PlayerCountAtStart + tournament.BoostedPotAtStart
            : entryCost * cfg.PaidPlayers.Count + boostedPot;

        var round = tournament == null
            ? "Registration"
            : tournament.CurrentRoundIndex == tournament.Rounds.Count - 1
                ? "The Final"
                : $"Round {tournament.CurrentRoundIndex + 1}";

        return new DeathrollTournamentGameInfoIpc
        {
            GameLabel      = DeathrollGameIds.DisplayName,
            Round          = round,
            BoostedPot     = boostedPot,
            TotalPot       = totalPot,
            EntryCost      = entryCost,
            PlayersEntered = players,
        };
    }
}
