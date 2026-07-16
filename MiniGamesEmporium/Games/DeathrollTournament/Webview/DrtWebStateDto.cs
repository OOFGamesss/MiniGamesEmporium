using System.Collections.Generic;

/// <summary>Wire payload pushed to the MiniGames Emporium API for the live Deathroll Tournament bracket page.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Webview;

public sealed class DrtWebStateDto
{
    public string Phase { get; set; } = "idle";
    public long EntryCost { get; set; }
    public long Pot { get; set; }
    public DrtWebPrizeDto? Prize { get; set; }
    public List<DrtWebPlayerDto> Players { get; set; } = new();
    public DrtWebBracketDto? Bracket { get; set; }
    public DrtWebActiveMatchDto? ActiveMatch { get; set; }
    public DrtWebBettingDto? Betting { get; set; }
    public DrtWebWinnerDto? Winner { get; set; }
}

public sealed class DrtWebPrizeDto
{
    public string Type { get; set; } = "gil";
    public string Label { get; set; } = string.Empty;
    public long GilAmount { get; set; }
}

public sealed class DrtWebPlayerDto
{
    public string Name { get; set; } = string.Empty;
    public bool Paid { get; set; }
    public bool Unlinked { get; set; }
}

public sealed class DrtWebBracketDto
{
    public List<int> BestOfPerRound { get; set; } = new();
    public int CurrentRoundIndex { get; set; }
    public int CurrentMatchIndex { get; set; }
    public List<List<DrtWebMatchDto>> Rounds { get; set; } = new();
}

public sealed class DrtWebMatchDto
{
    public string P1 { get; set; } = string.Empty;
    public string P2 { get; set; } = string.Empty;
    public string Winner { get; set; } = string.Empty;
    public int P1Wins { get; set; }
    public int P2Wins { get; set; }
    public bool Resolved { get; set; }
    public bool P1Bye { get; set; }
    public bool P2Bye { get; set; }
}

public sealed class DrtWebActiveMatchDto
{
    public string Phase { get; set; } = string.Empty;
    public string TurnPlayer { get; set; } = string.Empty;
    public int RollMax { get; set; }
    public int P1Wins { get; set; }
    public int P2Wins { get; set; }
    public List<DrtWebRollDto> RollLog { get; set; } = new();
}

public sealed class DrtWebRollDto
{
    public string Player { get; set; } = string.Empty;
    public int Max { get; set; }
    public int Value { get; set; }
}

public sealed class DrtWebBettingDto
{
    public bool Enabled { get; set; }
    public long BetUnit { get; set; }
    public long Pot { get; set; }
    public List<DrtWebBetDto> ConfirmedBets { get; set; } = new();
    public List<DrtWebPayoutDto> Payouts { get; set; } = new();
}

public sealed class DrtWebBetDto
{
    public string Bettor { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
}

public sealed class DrtWebPayoutDto
{
    public string Bettor { get; set; } = string.Empty;
    public long Share { get; set; }
    public long Paid { get; set; }
}

public sealed class DrtWebWinnerDto
{
    public string Name { get; set; } = string.Empty;
    public long PayoutGil { get; set; }
}
