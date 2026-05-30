using MiniGamesEmporium.Events;
using MiniGamesEmporium.Games.DeathrollTournament.Services;

/// <summary>Handles RandomNumber roll events for Deathroll Tournament, routing them to order rolls or game roll recording.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Events;
public sealed class DeathrollRollHandler : IChatRollHandler
{
    private readonly DeathrollTournamentService deathrollService;

    public DeathrollRollHandler(DeathrollTournamentService deathrollService)
    {
        this.deathrollService = deathrollService;
    }

    public void TryHandleRoll(string playerName, int rollValue, int rollMax)
    {
        if (!this.deathrollService.HasActiveTournament()) return;

        var name = playerName;
        if (string.IsNullOrEmpty(name))
        {
            var localName = MiniGamesEmporium.ObjectTable.LocalPlayer?.Name.TextValue;
            if (!string.IsNullOrEmpty(localName))
                name = localName;
        }

        if (!this.deathrollService.TryCatchNextMatchOrderRoll(name, rollValue, rollMax) &&
            !this.deathrollService.TryCatchNextGameOrderRoll(name, rollValue, rollMax))
            this.deathrollService.TryRecordRoll(name, rollValue, rollMax);
    }
}
