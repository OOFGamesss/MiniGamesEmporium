using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Actions;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Services;
using System;

/// <summary>Subscribes to Deathroll Tournament service events and automatically dispatches the configured announcements to chat when each fires.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Automation;
public sealed class DeathrollChatAutomation : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private readonly ChatQueueService chatQueue;

    public DeathrollChatAutomation(
        PluginConfiguration config,
        DeathrollTournamentService deathrollService,
        ChatQueueService chatQueue)
    {
        this.config           = config;
        this.deathrollService = deathrollService;
        this.chatQueue        = chatQueue;
        deathrollService.MatchStarted      += OnMatchStarted;
        deathrollService.TournamentWon     += OnTournamentWon;
        deathrollService.OrderRollTied     += OnOrderRollTied;
        deathrollService.OrderRollResolved += OnOrderRollResolved;
        deathrollService.GameWon           += OnGameWon;
        deathrollService.MatchWon          += OnMatchWon;
    }

    private void OnMatchStarted(string player1, string player2)
    {
        if (!this.config.DeathrollTournament.Chat.AutoAnnounceMatchup) return;
        AnnounceMatchup.Execute(player1, player2, this.config, this.chatQueue);
    }

    private void OnTournamentWon(string winner, long totalPot)
    {
        if (!this.config.DeathrollTournament.Chat.AutoAnnounceWinner) return;
        AnnounceTournamentWinner.Execute(winner, totalPot, this.config, this.chatQueue);
    }

    private void OnOrderRollTied(int tiedValue)
    {
        if (!this.config.DeathrollTournament.Chat.AutoAnnounceRerollRandom10) return;
        AnnounceRerollRandom10.Execute(tiedValue, this.config, this.chatQueue);
    }

    private void OnOrderRollResolved(string firstPlayer)
    {
        if (!this.config.DeathrollTournament.Chat.AutoAnnounceFirstPlayer) return;
        AnnounceFirstPlayer.Execute(firstPlayer, this.config, this.chatQueue);
    }

    private void OnGameWon(string roundWinner, int winnerWins, int loserWins, int gamesLeft)
    {
        if (!this.config.DeathrollTournament.Chat.AutoAnnounceRoundWin) return;
        AnnounceRoundWin.Execute(roundWinner, winnerWins, loserWins, gamesLeft, this.config, this.chatQueue);
    }

    private void OnMatchWon(string matchWinner, string matchLoser, int winnerWins, int loserWins)
    {
        if (!this.config.DeathrollTournament.Chat.AutoAnnounceMatchWin) return;
        AnnounceMatchWin.Execute(matchWinner, matchLoser, winnerWins, loserWins, this.config, this.chatQueue);
    }

    public void Dispose()
    {
        this.deathrollService.MatchStarted      -= OnMatchStarted;
        this.deathrollService.TournamentWon     -= OnTournamentWon;
        this.deathrollService.OrderRollTied     -= OnOrderRollTied;
        this.deathrollService.OrderRollResolved -= OnOrderRollResolved;
        this.deathrollService.GameWon           -= OnGameWon;
        this.deathrollService.MatchWon          -= OnMatchWon;
    }
}
