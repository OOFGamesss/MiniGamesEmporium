using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Services;

/// <summary>Captures the host's armed /random result as the raffle draw when a session is running.</summary>

namespace MiniGamesEmporium.Games.Raffle.Events;
public sealed class RaffleRollHandler : IChatRollHandler
{
    private readonly RaffleService raffleService;
    private readonly PlayerInfoService playerInfo;

    public RaffleRollHandler(RaffleService raffleService, PlayerInfoService playerInfo)
    {
        this.raffleService = raffleService;
        this.playerInfo    = playerInfo;
    }

    public void TryHandleRoll(string playerName, int rollValue, int rollMax)
    {
        if (!this.raffleService.IsSessionActive()) return;
        var state = this.raffleService.GetState();
        if (state == null || !state.IsDrawArmed) return;
        var name = string.IsNullOrEmpty(playerName) ? this.playerInfo.HostName : playerName;
        if (!this.playerInfo.IsHost(name)) return;
        this.raffleService.TryCaptureDrawRoll(rollValue);
    }
}
