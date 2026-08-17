using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Services;

/// <summary>Handles RandomNumber roll events for Deathroll Tournament, routing them to order rolls or game roll recording.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Events;
public sealed class DeathrollRollHandler : IChatRollHandler
{
    private readonly DeathrollTournamentService deathrollService;
    private readonly PlayerInfoService playerInfo;

    public DeathrollRollHandler(DeathrollTournamentService deathrollService, PlayerInfoService playerInfo)
    {
        this.deathrollService = deathrollService;
        this.playerInfo = playerInfo;
    }

    public string GameName => DeathrollGameIds.DisplayName;

    public void TryHandleRoll(string playerName, int rollValue, int rollMax)
    {
        if (!this.deathrollService.HasActiveTournament()) return;

        var name = string.IsNullOrEmpty(playerName) ? this.playerInfo.HostName : playerName;

        if (!this.deathrollService.TryCatchNextMatchOrderRoll(name, rollValue, rollMax) &&
            !this.deathrollService.TryCatchNextGameOrderRoll(name, rollValue, rollMax))
            this.deathrollService.TryRecordRoll(name, rollValue, rollMax);
    }
}
