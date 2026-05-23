using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.State;
using System;
using System.Collections.Generic;
using System.Linq;


/// <summary>Manages the full lifecycle of a Deathroll Tournament session: player registration, bracket generation with BYE seeding, match progression, order-roll and deathroll recording, best-of series tracking, and winner advancement.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Services;
public sealed class DeathrollTournamentService
{
    private readonly PluginConfiguration config;
    public event Action? SessionUpdated;
    public event Action<string, string>? MatchStarted;
    public event Action? MatchCompleted;
    public event Action<string, long>? TournamentWon;
    public event Action? NextMatchCatchTriggered;
    public event Action<int>? OrderRollTied;
    public event Action<string>? OrderRollResolved;
    public event Action<string, int, int, int>? GameWon;
    public event Action<string, string, int, int>? MatchWon;

    public DeathrollTournamentService(PluginConfiguration config)
    {
        this.config = config;
    }

    public bool IsSessionActive() => this.config.DeathrollSession != null;
    public bool HasActiveTournament() => this.config.DeathrollTournamentSession != null;
    public DeathrollTournamentState? GetState() => this.config.DeathrollTournamentSession;

    public void StartSession()
    {
        if (this.config.DeathrollSession != null) return;
        if (this.config.ActiveSession != null) return;
        var cfg = this.config.DeathrollTournament;
        cfg.PaidPlayers.Clear();
        this.config.DeathrollSession = new DeathrollSessionInfo
        {
            EntryCost  = cfg.EntryCost,
            BoostedPot = cfg.BoostedPot,
            StartedAt  = DateTime.UtcNow,
        };
        Save();
        SessionUpdated?.Invoke();
    }

    public void StopSession()
    {
        RecordSessionHistory();
        this.config.DeathrollTournamentSession = null;
        this.config.DeathrollSession           = null;
        this.config.DeathrollTournament.RegisteredPlayers.Clear();
        this.config.DeathrollTournament.PaidPlayers.Clear();
        Save();
        SessionUpdated?.Invoke();
    }

    private void RecordSessionHistory()
    {
        var session    = this.config.DeathrollSession;
        var tournament = this.config.DeathrollTournamentSession;
        if (session == null) return;
        var entryCost      = tournament?.EntryCostAtStart  ?? session.EntryCost;
        var boostedPot     = tournament?.BoostedPotAtStart ?? session.BoostedPot;
        var playerCount    = tournament?.PlayerCountAtStart ?? this.config.DeathrollTournament.PaidPlayers.Count;
        var amountInTrades = entryCost * playerCount;
        var totalPot       = amountInTrades + boostedPot;
        var winner         = tournament?.TournamentWinner ?? string.Empty;
        int? roundsPlayed  = null;
        int? matchesPlayed = null;
        if (tournament != null)
        {
            roundsPlayed   = tournament.Rounds.Count(r => r.Any(m => m.IsResolved && !DeathrollGameIds.IsBye(m.Player1) && !DeathrollGameIds.IsBye(m.Player2)));
            matchesPlayed  = tournament.Rounds.Sum(r => r.Count(m => m.IsResolved && !DeathrollGameIds.IsBye(m.Player1) && !DeathrollGameIds.IsBye(m.Player2)));
            if (roundsPlayed == 0)  roundsPlayed  = null;
            if (matchesPlayed == 0) matchesPlayed = null;
        }
        this.config.SessionHistory.Add(new SessionRecord
        {
            GameName       = DeathrollGameIds.DisplayName,
            Winner         = winner,
            BoostedPot     = boostedPot,
            AmountInTrades = amountInTrades,
            TotalPot       = totalPot,
            PlayersPlayed  = playerCount,
            RoundsPlayed   = roundsPlayed,
            MatchesPlayed  = matchesPlayed,
            Timestamp      = DateTime.UtcNow,
        });
    }

    public bool IsPaid(string playerEntry)
    {
        var name = ParseName(playerEntry);
        return this.config.DeathrollTournament.PaidPlayers
            .Any(p => ParseName(p).Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void MarkAsPaid(string playerEntry)
    {
        var name = ParseName(playerEntry);
        var list = this.config.DeathrollTournament.PaidPlayers;
        if (list.Any(p => ParseName(p).Equals(name, StringComparison.OrdinalIgnoreCase))) return;
        list.Add(name);
        Save();
        SessionUpdated?.Invoke();
    }

    public void TogglePaid(string playerEntry)
    {
        var name = ParseName(playerEntry);
        var list = this.config.DeathrollTournament.PaidPlayers;
        var existing = list.FirstOrDefault(p => ParseName(p).Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            list.Remove(existing);
        else
            list.Add(name);
        Save();
        SessionUpdated?.Invoke();
    }

    public void TryAutoMarkPaid(string tradePartner, long gilReceived)
    {
        if (!IsSessionActive()) return;
        var entryCost = this.config.DeathrollSession?.EntryCost ?? this.config.DeathrollTournament.EntryCost;
        if (gilReceived < entryCost) return;
        var registered = this.config.DeathrollTournament.RegisteredPlayers;
        var match = registered.FirstOrDefault(p => NamesMatch(p, tradePartner));
        if (match == null || IsPaid(match)) return;
        this.config.Transactions.Add(new TransactionRecord
        {
            PlayerName = ParseName(match),
            Amount     = (int)entryCost,
            Timestamp  = DateTime.UtcNow,
            GameName   = DeathrollGameIds.DisplayName,
        });
        MarkAsPaid(match);
    }

    public BracketMatch? GetCurrentMatch()
    {
        var state = this.config.DeathrollTournamentSession;
        if (state == null || state.Rounds.Count == 0) return null;
        var r = state.CurrentRoundIndex;
        var m = state.CurrentMatchIndex;
        if (r >= state.Rounds.Count || m >= state.Rounds[r].Count) return null;
        return state.Rounds[r][m];
    }

    public long ComputeTotalPot()
    {
        var tournament = this.config.DeathrollTournamentSession;
        if (tournament != null)
            return tournament.EntryCostAtStart * tournament.PlayerCountAtStart + tournament.BoostedPotAtStart;
        var activeSession = this.config.DeathrollSession;
        var cfg        = this.config.DeathrollTournament;
        var entryCost  = activeSession?.EntryCost  ?? cfg.EntryCost;
        var boostedPot = activeSession?.BoostedPot ?? cfg.BoostedPot;
        return entryCost * cfg.PaidPlayers.Count + boostedPot;
    }

    public List<string> GetUnpaidRegisteredPlayers()
    {
        return this.config.DeathrollTournament.RegisteredPlayers
            .Where(p => !IsPaid(p))
            .Select(ParseName)
            .ToList();
    }

    public void AddPlayer(string playerEntry)
    {
        if (string.IsNullOrWhiteSpace(playerEntry)) return;
        var name = ParseName(playerEntry.Trim());
        var list = this.config.DeathrollTournament.RegisteredPlayers;
        if (list.Any(p => ParseName(p).Equals(name, StringComparison.OrdinalIgnoreCase))) return;
        list.Add(playerEntry.Trim());
        Save();
    }

    public void RemovePlayer(int index)
    {
        var list = this.config.DeathrollTournament.RegisteredPlayers;
        if (index < 0 || index >= list.Count) return;
        list.RemoveAt(index);
        Save();
    }

    public void MovePlayerUp(int index)
    {
        var list = this.config.DeathrollTournament.RegisteredPlayers;
        if (index <= 0 || index >= list.Count) return;
        (list[index], list[index - 1]) = (list[index - 1], list[index]);
        Save();
    }

    public void MovePlayerDown(int index)
    {
        var list = this.config.DeathrollTournament.RegisteredPlayers;
        if (index < 0 || index >= list.Count - 1) return;
        (list[index], list[index + 1]) = (list[index + 1], list[index]);
        Save();
    }

    public void ShufflePlayers()
    {
        var list = this.config.DeathrollTournament.RegisteredPlayers;
        var rng = new Random();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        Save();
    }

    public void StartTournament()
    {
        if (this.config.DeathrollTournamentSession != null) return;
        var cfg           = this.config.DeathrollTournament;
        var activeSession = this.config.DeathrollSession;
        var players = cfg.RegisteredPlayers.Where(IsPaid).ToList();
        if (players.Count < 2) return;
        var rounds = GenerateBracket(players);
        var bestOf = cfg.BestOfPerRound.ToList();
        var state = new DeathrollTournamentState
        {
            EntryCostAtStart   = activeSession?.EntryCost  ?? cfg.EntryCost,
            BoostedPotAtStart  = activeSession?.BoostedPot ?? cfg.BoostedPot,
            PlayerCountAtStart = players.Count,
            StartedAt          = DateTime.UtcNow,
            Rounds             = rounds,
            BestOfPerRound     = bestOf,
        };
        PositionToNextActiveMatch(state);
        this.config.DeathrollTournamentSession = state;
        Save();
        SessionUpdated?.Invoke();
    }

    public void StopTournament()
    {
        this.config.DeathrollTournamentSession = null;
        Save();
        SessionUpdated?.Invoke();
    }

    public void StartCurrentMatch()
    {
        var state = this.config.DeathrollTournamentSession;
        if (state == null || state.ActiveMatchPhase != MatchPhase.NotStarted) return;
        var match = GetCurrentMatch(state);
        state.ActiveMatchPhase   = MatchPhase.DeterminingOrder;
        state.OrderRollPlayer1   = 0;
        state.OrderRollPlayer2   = 0;
        state.LastOrderTiedValue = 0;
        Save();
        SessionUpdated?.Invoke();
        if (match != null) MatchStarted?.Invoke(match.Player1, match.Player2);
    }

    public void StartNextGame()
    {
        var state = this.config.DeathrollTournamentSession;
        if (state == null || state.ActiveMatchPhase != MatchPhase.GameOver) return;
        state.ActiveMatchPhase        = MatchPhase.DeterminingOrder;
        state.OrderRollPlayer1        = 0;
        state.OrderRollPlayer2        = 0;
        state.LastOrderTiedValue      = 0;
        state.ActiveRollLog           = new();
        state.CurrentDeathrollMax     = 0;
        state.CurrentTurnPlayerName   = string.Empty;
        Save();
        SessionUpdated?.Invoke();
    }

    public void MoveToNextMatch()
    {
        var state = this.config.DeathrollTournamentSession;
        if (state == null || state.ActiveMatchPhase != MatchPhase.MatchComplete) return;
        PositionToNextActiveMatch(state);
        ResetActiveMatchState(state);
        Save();
        SessionUpdated?.Invoke();
    }

    public void ManuallySetWinner(string playerEntry)
    {
        var state = this.config.DeathrollTournamentSession;
        if (state == null) return;
        var match = GetCurrentMatch(state);
        if (match == null || match.IsResolved) return;
        SetMatchWinner(state, match, playerEntry);
        Save();
        SessionUpdated?.Invoke();
    }

    public void ManuallyAddRoundWin(string playerEntry)
    {
        var state = this.config.DeathrollTournamentSession;
        if (state == null) return;
        var match = GetCurrentMatch(state);
        if (match == null || match.IsResolved || state.ActiveMatchPhase == MatchPhase.MatchComplete) return;
        var winnerIsP1 = NamesMatch(playerEntry, match.Player1);
        if (winnerIsP1)
            state.ActiveMatchPlayer1Wins++;
        else
            state.ActiveMatchPlayer2Wins++;
        var roundIdx   = state.CurrentRoundIndex;
        var bestOf     = roundIdx < state.BestOfPerRound.Count ? state.BestOfPerRound[roundIdx] : 1;
        var winsNeeded = bestOf / 2 + 1;
        if (state.ActiveMatchPlayer1Wins >= winsNeeded)
            SetMatchWinner(state, match, match.Player1);
        else if (state.ActiveMatchPlayer2Wins >= winsNeeded)
            SetMatchWinner(state, match, match.Player2);
        else
        {
            var winner     = winnerIsP1 ? match.Player1 : match.Player2;
            var winnerWins = winnerIsP1 ? state.ActiveMatchPlayer1Wins : state.ActiveMatchPlayer2Wins;
            var loserWins  = winnerIsP1 ? state.ActiveMatchPlayer2Wins : state.ActiveMatchPlayer1Wins;
            var gamesLeft  = bestOf - (state.ActiveMatchPlayer1Wins + state.ActiveMatchPlayer2Wins);
            GameWon?.Invoke(winner, winnerWins, loserWins, gamesLeft);
            state.ActiveMatchPhase      = MatchPhase.DeterminingOrder;
            state.OrderRollPlayer1      = 0;
            state.OrderRollPlayer2      = 0;
            state.LastOrderTiedValue    = 0;
            state.ActiveRollLog         = new();
            state.CurrentDeathrollMax   = 0;
            state.CurrentTurnPlayerName = string.Empty;
        }
        Save();
        SessionUpdated?.Invoke();
    }

    public void TryRecordRoll(string senderName, int rollValue, int rollMax)
    {
        var state = this.config.DeathrollTournamentSession;
        if (state == null) return;
        if (state.ActiveMatchPhase == MatchPhase.DeterminingOrder)
            HandleOrderRoll(state, senderName, rollValue, rollMax);
        else if (state.ActiveMatchPhase == MatchPhase.Deathrolling)
            HandleDeathRoll(state, senderName, rollValue, rollMax);
    }

    public bool TryCatchNextMatchOrderRoll(string senderName, int rollValue, int rollMax)
    {
        if (!this.config.DeathrollTournament.AutoCatchNextRound) return false;
        if (rollMax != 10) return false;
        var state = this.config.DeathrollTournamentSession;
        if (state == null || state.ActiveMatchPhase != MatchPhase.MatchComplete) return false;
        if (state.TournamentWinner != null) return false;
        var next = FindNextActiveMatch(state);
        if (next == null) return false;
        var nextMatch = state.Rounds[next.Value.r][next.Value.m];
        if (!NamesMatch(senderName, nextMatch.Player1) && !NamesMatch(senderName, nextMatch.Player2)) return false;
        NextMatchCatchTriggered?.Invoke();
        MoveToNextMatch();
        StartCurrentMatch();
        TryRecordRoll(senderName, rollValue, rollMax);
        return true;
    }

    public bool TryCatchNextGameOrderRoll(string senderName, int rollValue, int rollMax)
    {
        if (!this.config.DeathrollTournament.AutoCatchNextRound) return false;
        if (rollMax != 10) return false;
        var state = this.config.DeathrollTournamentSession;
        if (state == null || state.ActiveMatchPhase != MatchPhase.GameOver) return false;
        var match = GetCurrentMatch(state);
        if (match == null || match.IsResolved) return false;
        if (!NamesMatch(senderName, match.Player1) && !NamesMatch(senderName, match.Player2)) return false;
        StartNextGame();
        TryRecordRoll(senderName, rollValue, rollMax);
        return true;
    }

    private void HandleOrderRoll(DeathrollTournamentState state, string senderName, int rollValue, int rollMax)
    {
        if (rollMax != 10) return;
        var match = GetCurrentMatch(state);
        if (match == null) return;
        var p1 = ParseName(match.Player1);
        var p2 = ParseName(match.Player2);
        if (NamesMatch(senderName, p1) && state.OrderRollPlayer1 == 0)
            state.OrderRollPlayer1 = rollValue;
        else if (NamesMatch(senderName, p2) && state.OrderRollPlayer2 == 0)
            state.OrderRollPlayer2 = rollValue;
        else
            return;
        if (state.OrderRollPlayer1 != 0 && state.OrderRollPlayer2 != 0)
        {
            if (state.OrderRollPlayer1 == state.OrderRollPlayer2)
            {
                var tiedValue = state.OrderRollPlayer1;
                state.OrderRollPlayer1   = 0;
                state.OrderRollPlayer2   = 0;
                state.LastOrderTiedValue = tiedValue;
                Save();
                SessionUpdated?.Invoke();
                OrderRollTied?.Invoke(tiedValue);
                return;
            }
            state.LastOrderTiedValue = 0;
            FinaliseOrderRolls(state, match);
            Save();
            SessionUpdated?.Invoke();
            OrderRollResolved?.Invoke(state.CurrentTurnPlayerName);
            return;
        }
        Save();
        SessionUpdated?.Invoke();
    }

    private static void FinaliseOrderRolls(DeathrollTournamentState state, BracketMatch match)
    {
        var p1GoesFirst = state.OrderRollPlayer1 >= state.OrderRollPlayer2;
        state.CurrentTurnPlayerName   = p1GoesFirst ? match.Player1 : match.Player2;
        state.CurrentDeathrollMax     = 0;
        state.ActiveMatchPhase        = MatchPhase.Deathrolling;
    }

    private void HandleDeathRoll(DeathrollTournamentState state, string senderName, int rollValue, int rollMax)
    {
        if (!NamesMatch(senderName, state.CurrentTurnPlayerName)) return;
        if (state.CurrentDeathrollMax == 0 && rollMax != 0) return;
        if (state.CurrentDeathrollMax != 0 && rollMax != state.CurrentDeathrollMax) return;
        var match = GetCurrentMatch(state);
        if (match == null) return;
        state.ActiveRollLog.Add(new DeathrollEntry
        {
            PlayerName = ParseName(state.CurrentTurnPlayerName),
            RollMax    = state.CurrentDeathrollMax == 0 ? 1000 : state.CurrentDeathrollMax,
            RollValue  = rollValue,
        });
        if (rollValue == 1)
            HandleGameLoss(state, match);
        else
            SwitchTurn(state, match, rollValue);
        Save();
        SessionUpdated?.Invoke();
    }

    private static void SwitchTurn(DeathrollTournamentState state, BracketMatch match, int newMax)
    {
        var wasP1 = NamesMatch(state.CurrentTurnPlayerName, match.Player1);
        state.CurrentDeathrollMax   = newMax;
        state.CurrentTurnPlayerName = wasP1 ? match.Player2 : match.Player1;
    }

    private void HandleGameLoss(DeathrollTournamentState state, BracketMatch match)
    {
        var loserIsP1 = NamesMatch(state.CurrentTurnPlayerName, match.Player1);
        if (loserIsP1)
            state.ActiveMatchPlayer2Wins++;
        else
            state.ActiveMatchPlayer1Wins++;
        var roundIdx    = state.CurrentRoundIndex;
        var bestOf      = roundIdx < state.BestOfPerRound.Count ? state.BestOfPerRound[roundIdx] : 1;
        var winsNeeded  = bestOf / 2 + 1;
        if (state.ActiveMatchPlayer1Wins >= winsNeeded)
            SetMatchWinner(state, match, match.Player1);
        else if (state.ActiveMatchPlayer2Wins >= winsNeeded)
            SetMatchWinner(state, match, match.Player2);
        else
        {
            state.ActiveMatchPhase = MatchPhase.GameOver;
            var roundWinner = loserIsP1 ? match.Player2 : match.Player1;
            var winnerWins  = loserIsP1 ? state.ActiveMatchPlayer2Wins : state.ActiveMatchPlayer1Wins;
            var loserWins   = loserIsP1 ? state.ActiveMatchPlayer1Wins : state.ActiveMatchPlayer2Wins;
            var gamesLeft   = bestOf - (state.ActiveMatchPlayer1Wins + state.ActiveMatchPlayer2Wins);
            GameWon?.Invoke(roundWinner, winnerWins, loserWins, gamesLeft);
        }
    }

    private void SetMatchWinner(DeathrollTournamentState state, BracketMatch match, string winner)
    {
        var loser        = NamesMatch(winner, match.Player1) ? match.Player2 : match.Player1;
        var winnerIsP1   = NamesMatch(winner, match.Player1);
        var winnerWins   = winnerIsP1 ? state.ActiveMatchPlayer1Wins : state.ActiveMatchPlayer2Wins;
        var loserWins    = winnerIsP1 ? state.ActiveMatchPlayer2Wins : state.ActiveMatchPlayer1Wins;
        match.Winner      = winner;
        match.Player1Wins = state.ActiveMatchPlayer1Wins;
        match.Player2Wins = state.ActiveMatchPlayer2Wins;
        match.IsResolved  = true;
        state.ActiveMatchPhase = MatchPhase.MatchComplete;
        var roundIdx  = state.CurrentRoundIndex;
        var matchIdx  = state.CurrentMatchIndex;
        AdvanceWinner(state.Rounds, roundIdx, matchIdx, winner);
        MatchWon?.Invoke(winner, loser, winnerWins, loserWins);
        if (roundIdx + 1 >= state.Rounds.Count || state.Rounds[roundIdx + 1].All(m => m.IsResolved))
        {
            if (FindNextActiveMatch(state) == null)
            {
                state.TournamentWinner = winner;
                TournamentWon?.Invoke(winner, ComputeTotalPot());
                return;
            }
        }
        MatchCompleted?.Invoke();
    }

    private static void AdvanceWinner(List<List<BracketMatch>> rounds, int fromRound, int fromMatch, string winner)
    {
        if (fromRound + 1 >= rounds.Count) return;
        var nextMatch = rounds[fromRound + 1][fromMatch / 2];
        if (fromMatch % 2 == 0)
            nextMatch.Player1 = winner;
        else
            nextMatch.Player2 = winner;
        TryAutoResolve(rounds, fromRound + 1, fromMatch / 2);
    }

    private static void TryAutoResolve(List<List<BracketMatch>> rounds, int roundIdx, int matchIdx)
    {
        var m = rounds[roundIdx][matchIdx];
        if (m.IsResolved || string.IsNullOrEmpty(m.Player1) || string.IsNullOrEmpty(m.Player2)) return;
        var p1Bye = DeathrollGameIds.IsBye(m.Player1);
        var p2Bye = DeathrollGameIds.IsBye(m.Player2);
        if (!p1Bye && !p2Bye) return;
        m.Winner     = p1Bye ? m.Player2 : m.Player1;
        m.IsResolved = true;
        AdvanceWinner(rounds, roundIdx, matchIdx, m.Winner);
    }

    private static void PositionToNextActiveMatch(DeathrollTournamentState state)
    {
        var next = FindNextActiveMatch(state);
        if (next == null) return;
        state.CurrentRoundIndex = next.Value.r;
        state.CurrentMatchIndex = next.Value.m;
    }

    private static (int r, int m)? FindNextActiveMatch(DeathrollTournamentState state)
    {
        for (var r = 0; r < state.Rounds.Count; r++)
        {
            for (var m = 0; m < state.Rounds[r].Count; m++)
            {
                var match = state.Rounds[r][m];
                if (match.IsResolved) continue;
                if (string.IsNullOrEmpty(match.Player1) || string.IsNullOrEmpty(match.Player2)) continue;
                return (r, m);
            }
        }
        return null;
    }

    private static List<List<BracketMatch>> GenerateBracket(List<string> players)
    {
        var size   = NextPowerOf2(players.Count);
        var padded = players.ToList();
        while (padded.Count < size) padded.Add(DeathrollGameIds.ByeSlot);
        var rounds  = new List<List<BracketMatch>>();
        var firstRound = new List<BracketMatch>();
        for (var i = 0; i < size; i += 2)
        {
            var match = new BracketMatch { Player1 = padded[i], Player2 = padded[i + 1] };
            AutoResolveIfBye(match);
            firstRound.Add(match);
        }
        rounds.Add(firstRound);
        var slots = firstRound.Count / 2;
        while (slots >= 1)
        {
            var r = new List<BracketMatch>(slots);
            for (var i = 0; i < slots; i++) r.Add(new BracketMatch());
            rounds.Add(r);
            slots /= 2;
        }
        PropagateInitialByes(rounds);
        return rounds;
    }

    private static void AutoResolveIfBye(BracketMatch match)
    {
        var p1Bye = DeathrollGameIds.IsBye(match.Player1);
        var p2Bye = DeathrollGameIds.IsBye(match.Player2);
        if (!p1Bye && !p2Bye) return;
        match.Winner     = p1Bye ? match.Player2 : match.Player1;
        match.IsResolved = true;
    }

    private static void PropagateInitialByes(List<List<BracketMatch>> rounds)
    {
        for (var r = 0; r < rounds.Count - 1; r++)
            for (var m = 0; m < rounds[r].Count; m++)
            {
                var match = rounds[r][m];
                if (!match.IsResolved || string.IsNullOrEmpty(match.Winner)) continue;
                AdvanceWinner(rounds, r, m, match.Winner);
            }
    }

    private static void ResetActiveMatchState(DeathrollTournamentState state)
    {
        state.OrderRollPlayer1       = 0;
        state.OrderRollPlayer2       = 0;
        state.LastOrderTiedValue     = 0;
        state.CurrentTurnPlayerName  = string.Empty;
        state.CurrentDeathrollMax    = 0;
        state.ActiveRollLog          = new();
        state.ActiveMatchPlayer1Wins = 0;
        state.ActiveMatchPlayer2Wins = 0;
        state.ActiveMatchPhase       = MatchPhase.NotStarted;
    }

    private static BracketMatch? GetCurrentMatch(DeathrollTournamentState state)
    {
        var r = state.CurrentRoundIndex;
        var m = state.CurrentMatchIndex;
        if (r >= state.Rounds.Count || m >= state.Rounds[r].Count) return null;
        return state.Rounds[r][m];
    }

    private static int NextPowerOf2(int n)
    {
        if (n <= 1) return 2;
        var p = 1;
        while (p < n) p <<= 1;
        return p;
    }

    private static string ParseName(string entry)
    {
        var at = entry.IndexOf('@');
        return at < 0 ? entry.Trim() : entry[..at].Trim();
    }

    private static bool NamesMatch(string a, string b)
    {
        var parsedA = ParseName(a);
        var parsedB = ParseName(b);
        if (parsedA.Equals(parsedB, StringComparison.OrdinalIgnoreCase)) return true;
        return CrossWorldNamesMatch(parsedA, parsedB) || CrossWorldNamesMatch(parsedB, parsedA);
    }

    private static bool CrossWorldNamesMatch(string fromChat, string registered)
    {
        var chatParts = fromChat.Split(' ');
        var regParts  = registered.Split(' ');
        if (chatParts.Length != 2 || regParts.Length != 2) return false;
        if (!chatParts[0].Equals(regParts[0], StringComparison.OrdinalIgnoreCase)) return false;
        return chatParts[1].StartsWith(regParts[1], StringComparison.OrdinalIgnoreCase)
            && chatParts[1].Length > regParts[1].Length
            && char.IsUpper(chatParts[1][regParts[1].Length]);
    }

    private void Save() => this.config.Save();
}
