using System;
using System.Collections.Generic;
using System.Linq;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Services;

/// <summary>Builds the DrtWebStateDto wire payload from the live Deathroll Tournament config and state.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Webview;

public static class DrtWebPayloadBuilder
{
    private const int MaxRollLogEntries = 50;

    public static DrtWebStateDto Build(PluginConfiguration config)
    {
        var tournament = config.DeathrollTournamentSession;
        if (tournament != null)
            return BuildTournament(config, tournament);
        if (config.DeathrollSession != null)
            return BuildRegistration(config);
        return new DrtWebStateDto { Phase = "idle" };
    }

    private static DrtWebStateDto BuildRegistration(PluginConfiguration config)
    {
        var session = config.DeathrollSession!;
        var cfg     = config.DeathrollTournament;
        return new DrtWebStateDto
        {
            Phase     = "registration",
            EntryCost = session.EntryCost,
            Pot       = DeathrollTournamentService.ComputeTotalPot(config),
            Prize     = BuildPrize(config, session.PrizeType),
            Players   = BuildPlayers(cfg),
            Betting   = BuildRegistrationBetting(config),
        };
    }

    private static DrtWebStateDto BuildTournament(PluginConfiguration config, DeathrollTournamentState state)
    {
        var complete = state.TournamentWinner != null;
        var dto = new DrtWebStateDto
        {
            Phase     = complete ? "complete" : "tournament",
            EntryCost = state.EntryCostAtStart,
            Pot       = DeathrollTournamentService.ComputeTotalPot(config),
            Prize     = BuildPrize(config, state.PrizeTypeAtStart),
            Players   = BuildPlayers(config.DeathrollTournament),
            Bracket   = BuildBracket(state),
            Betting   = BuildTournamentBetting(state),
        };
        if (complete)
        {
            dto.Winner = new DrtWebWinnerDto
            {
                Name      = PlayerInfoService.StripWorld(state.TournamentWinner!),
                PayoutGil = state.WinnerPayoutGil,
            };
        }
        else
        {
            dto.ActiveMatch = BuildActiveMatch(state);
        }
        return dto;
    }

    private static DrtWebPrizeDto BuildPrize(PluginConfiguration config, DeathrollPrizeType prizeType)
    {
        return new DrtWebPrizeDto
        {
            Type      = prizeType switch
            {
                DeathrollPrizeType.Item   => "item",
                DeathrollPrizeType.Custom => "custom",
                _                         => "gil",
            },
            Label     = DeathrollTournamentService.GetPrizeLabel(config),
            GilAmount = prizeType == DeathrollPrizeType.Gil ? DeathrollTournamentService.ComputeTotalPot(config) : 0,
        };
    }

    private static List<DrtWebPlayerDto> BuildPlayers(Games.DeathrollTournament.Config.DeathrollTournamentConfig cfg)
    {
        return cfg.RegisteredPlayers.Select(entry =>
        {
            var name = PlayerInfoService.StripWorld(entry);
            return new DrtWebPlayerDto
            {
                Name     = name,
                Paid     = cfg.PaidPlayers.Any(p => PlayerInfoService.StripWorld(p).Equals(name, StringComparison.OrdinalIgnoreCase)),
                Unlinked = cfg.UnverifiedPlayers.Any(u => u.Equals(name, StringComparison.OrdinalIgnoreCase)),
            };
        }).ToList();
    }

    private static DrtWebBracketDto BuildBracket(DeathrollTournamentState state)
    {
        return new DrtWebBracketDto
        {
            BestOfPerRound    = state.BestOfPerRound.ToList(),
            CurrentRoundIndex = state.CurrentRoundIndex,
            CurrentMatchIndex = state.CurrentMatchIndex,
            Rounds            = state.Rounds
                .Select(round => round.Select(BuildMatch).ToList())
                .ToList(),
        };
    }

    private static DrtWebMatchDto BuildMatch(BracketMatch match)
    {
        var p1Bye = DeathrollGameIds.IsBye(match.Player1);
        var p2Bye = DeathrollGameIds.IsBye(match.Player2);
        return new DrtWebMatchDto
        {
            P1       = p1Bye ? string.Empty : PlayerInfoService.StripWorld(match.Player1),
            P2       = p2Bye ? string.Empty : PlayerInfoService.StripWorld(match.Player2),
            Winner   = DeathrollGameIds.IsBye(match.Winner) ? string.Empty : PlayerInfoService.StripWorld(match.Winner),
            P1Wins   = match.Player1Wins,
            P2Wins   = match.Player2Wins,
            Resolved = match.IsResolved,
            P1Bye    = p1Bye,
            P2Bye    = p2Bye,
        };
    }

    private static DrtWebActiveMatchDto BuildActiveMatch(DeathrollTournamentState state)
    {
        return new DrtWebActiveMatchDto
        {
            Phase      = state.ActiveMatchPhase.ToString().ToLowerInvariant(),
            TurnPlayer = PlayerInfoService.StripWorld(state.CurrentTurnPlayerName),
            RollMax    = state.CurrentDeathrollMax,
            P1Wins     = state.ActiveMatchPlayer1Wins,
            P2Wins     = state.ActiveMatchPlayer2Wins,
            RollLog    = state.ActiveRollLog
                .Skip(Math.Max(0, state.ActiveRollLog.Count - MaxRollLogEntries))
                .Select(r => new DrtWebRollDto
                {
                    Player = PlayerInfoService.StripWorld(r.PlayerName),
                    Max    = r.RollMax,
                    Value  = r.RollValue,
                })
                .ToList(),
        };
    }

    private static DrtWebBettingDto BuildRegistrationBetting(PluginConfiguration config)
    {
        var session = config.DeathrollSession!;
        var cfg     = config.DeathrollTournament;
        if (!session.BettingEnabled) return new DrtWebBettingDto { Enabled = false };

        var confirmed = cfg.Bets
            .Where(b => b.IsPaid && !b.NeedsReview)
            .Select(b => new DrtWebBetDto
            {
                Bettor = PlayerInfoService.StripWorld(b.BettorName),
                Target = PlayerInfoService.StripWorld(b.TargetName),
            })
            .ToList();
        return new DrtWebBettingDto
        {
            Enabled       = true,
            BetUnit       = session.BetUnit,
            Pot           = session.BetUnit * confirmed.Count,
            ConfirmedBets = confirmed,
        };
    }

    private static DrtWebBettingDto BuildTournamentBetting(DeathrollTournamentState state)
    {
        if (!state.BettingEnabledAtStart) return new DrtWebBettingDto { Enabled = false };
        return new DrtWebBettingDto
        {
            Enabled       = true,
            BetUnit       = state.BetUnitAtStart,
            Pot           = state.BettingPotAtStart,
            ConfirmedBets = state.ConfirmedBets
                .Select(b => new DrtWebBetDto
                {
                    Bettor = PlayerInfoService.StripWorld(b.BettorName),
                    Target = PlayerInfoService.StripWorld(b.TargetName),
                })
                .ToList(),
            Payouts       = state.BetPayouts
                .Select(p => new DrtWebPayoutDto
                {
                    Bettor = PlayerInfoService.StripWorld(p.BettorName),
                    Share  = p.ShareGil,
                    Paid   = p.PaidGil,
                })
                .ToList(),
        };
    }
}
