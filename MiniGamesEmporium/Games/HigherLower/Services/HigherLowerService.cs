using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.HigherLower.Models;
using MiniGamesEmporium.Games.HigherLower.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Manages the full lifecycle of a Higher/Lower game session.</summary>

namespace MiniGamesEmporium.Games.HigherLower.Services;
public sealed class HigherLowerService : IDisposable
{
    private readonly PluginConfiguration config;
    private readonly HistoryService historyService;
    private readonly IChatGui chatGui;
    private readonly SessionService sessionService;
    private readonly List<string> _gameLog = [];

    public event Action? SessionUpdated;
    public event Action<string>? PaymentVerified;
    public event Action<string, int>? WinDetected;
    public event Action<string, int>? SessionLost;
    public event Action<string, int>? RoundCorrect;
    public event Action<int>? RollAwaitingGuess;

    public HigherLowerService(PluginConfiguration config, HistoryService historyService, IChatGui chatGui, SessionService sessionService)
    {
        this.config         = config;
        this.historyService = historyService;
        this.chatGui        = chatGui;
        this.sessionService = sessionService;
        this.chatGui.ChatMessage += OnChatMessage;
    }

    public bool IsSessionActive() => this.config.HigherLowerActiveSession != null;

    public ActiveSession? GetActiveSession() => this.config.HigherLowerActiveSession;

    public HigherLowerTurnState? GetActiveTurn() => this.config.HigherLowerSession;

    public IReadOnlyList<string> GetGameLog() => this._gameLog;

    public void StartSession()
    {
        if (this.config.HigherLowerActiveSession != null) return;
        this.config.HigherLowerActiveSession = new ActiveSession
        {
            GameName        = HigherLowerGameIds.DisplayName,
            PlayerName      = HigherLowerGameIds.NoPlayerSelectedPlaceholder,
            PlayerWorld     = string.Empty,
            PlayerSet       = false,
            PaymentVerified = false,
            AmountTraded    = 0,
            RollLog         = [],
            StartedAt       = DateTime.UtcNow,
        };
        this.config.HigherLowerSession            = null;
        this.config.HigherLower.SessionFinished   = false;
        this.config.HigherLower.WinnerPayouts.Clear();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void CancelSession()
    {
        if (!IsSessionActive()) return;
        WriteLeaderboardHistory();
        this.config.HigherLower.SessionLeaderboard.Clear();
        this.config.HigherLower.SessionTradedTotal = 0L;
        this.config.HigherLower.PlayersPlayed      = 0;
        this.config.HigherLower.SessionFinished    = false;
        this.config.HigherLower.WinnerPayouts.Clear();
        this.config.HigherLowerActiveSession = null;
        this.config.HigherLowerSession       = null;
        this._gameLog.Clear();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public bool IsSessionFinished() => IsSessionActive() && this.config.HigherLower.SessionFinished;

    public void FinishSession()
    {
        if (!IsSessionActive()) return;
        var winners = GetSessionWinners();
        this.config.HigherLower.WinnerPayouts = winners
            .Select(w => new HigherLowerWinnerPayout { PlayerName = w.Name, PaidGil = 0L })
            .ToList();
        this.config.HigherLower.SessionFinished = true;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void ResumeFinishedSession()
    {
        if (!IsSessionFinished()) return;
        this.config.HigherLower.SessionFinished = false;
        this.config.HigherLower.WinnerPayouts.Clear();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public List<(string Name, int Rounds)> GetSessionWinners()
    {
        var board = this.config.HigherLower.SessionLeaderboard;
        if (board.Count == 0) return [];

        var groups = new Dictionary<string, (int Best, DateTime FirstBestAt, bool HasWin)>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in board)
        {
            if (!groups.TryGetValue(e.PlayerName, out var g))
            {
                groups[e.PlayerName] = (e.RoundsCorrect, e.PlayedAt, e.IsWinner);
                continue;
            }
            var firstBestAt = e.RoundsCorrect > g.Best ? e.PlayedAt
                            : e.RoundsCorrect == g.Best && e.PlayedAt < g.FirstBestAt ? e.PlayedAt
                            : g.FirstBestAt;
            groups[e.PlayerName] = (Math.Max(g.Best, e.RoundsCorrect), firstBestAt, g.HasWin || e.IsWinner);
        }

        if (this.config.HigherLower.AllowMultipleWinners)
        {
            var flagged = groups.Where(kv => kv.Value.HasWin)
                .OrderByDescending(kv => kv.Value.Best).ThenBy(kv => kv.Value.FirstBestAt)
                .Select(kv => (kv.Key, kv.Value.Best)).ToList();
            if (flagged.Count > 0) return flagged;
            var max = groups.Max(kv => kv.Value.Best);
            return groups.Where(kv => kv.Value.Best == max)
                .OrderBy(kv => kv.Value.FirstBestAt)
                .Select(kv => (kv.Key, kv.Value.Best)).ToList();
        }

        var leader = groups.OrderByDescending(kv => kv.Value.Best).ThenBy(kv => kv.Value.FirstBestAt).First();
        return [(leader.Key, leader.Value.Best)];
    }

    public long GetSessionWinnerShare()
    {
        var pot   = GetTotalPot();
        var count = this.config.HigherLower.WinnerPayouts.Count;
        return count > 0 ? pot / count : pot;
    }

    public long GetWinnerPaid(string displayName) =>
        this.config.HigherLower.WinnerPayouts
            .FirstOrDefault(w => w.PlayerName.Equals(displayName, StringComparison.OrdinalIgnoreCase))?.PaidGil ?? 0L;

    public long GetWinnerRemaining(string displayName) =>
        Math.Max(0L, GetSessionWinnerShare() - GetWinnerPaid(displayName));

    public void SetPlayer(string charName, string worldName)
    {
        var session = GetActiveSession();
        if (session == null || session.PaymentVerified) return;
        session.PlayerName  = charName.Trim();
        session.PlayerWorld = worldName.Trim();
        session.PlayerSet   = true;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void UnsetPlayer()
    {
        var session = GetActiveSession();
        if (session == null || session.PaymentVerified) return;
        session.PlayerName       = HigherLowerGameIds.NoPlayerSelectedPlaceholder;
        session.PlayerWorld      = string.Empty;
        session.PlayerSet        = false;
        session.AmountTraded     = 0;
        session.PaidByPlayerName = string.Empty;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public string GetBuyer() => GetActiveSession()?.PaidByPlayerName ?? string.Empty;

    public void SetBuyer(string fullName)
    {
        var session = GetActiveSession();
        if (session == null || string.IsNullOrWhiteSpace(fullName)) return;
        session.PaidByPlayerName = fullName.Trim();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void ClearBuyer()
    {
        var session = GetActiveSession();
        if (session == null) return;
        session.PaidByPlayerName = string.Empty;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void TryVerifyPayment(string playerName, int amount, string playerWorld)
    {
        var session = GetActiveSession();
        if (session == null || session.PaymentVerified) return;
        var buyerBase    = PlayerInfoService.StripWorld(session.PaidByPlayerName);
        var isBuyerTrade = !string.IsNullOrEmpty(session.PaidByPlayerName)
                           && buyerBase.Equals(playerName, StringComparison.OrdinalIgnoreCase);
        var isPlaceholder = !session.PlayerSet;
        if (!isPlaceholder && !isBuyerTrade && !TradeNameMatchesSession(session, playerName)) return;
        if (isPlaceholder && !isBuyerTrade)
        {
            session.PlayerName  = playerName;
            session.PlayerWorld = string.IsNullOrEmpty(playerWorld) ? session.PlayerWorld : playerWorld;
            session.PlayerSet   = true;
        }
        session.AmountTraded += amount;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void TryRecordWinnerPayout(string partnerName, long amountSent)
    {
        if (!IsSessionFinished()) return;
        var payout = this.config.HigherLower.WinnerPayouts
            .FirstOrDefault(w => PlayerInfoService.StripWorld(w.PlayerName).Equals(partnerName, StringComparison.OrdinalIgnoreCase));
        if (payout == null) return;
        payout.PaidGil += amountSent;
        payout.PayoutTransactionId = PayoutTransactionRecorder.Record(this.historyService, HigherLowerGameIds.DisplayName, payout.PlayerName, payout.PaidGil, payout.PayoutTransactionId);
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void StartGame()
    {
        var session = GetActiveSession();
        if (session == null || session.PaymentVerified) return;
        session.PaymentVerified = true;
        this.config.HigherLower.PlayersPlayed++;
        this.config.HigherLower.SessionTradedTotal += session.AmountTraded;
        var transactionName = !string.IsNullOrEmpty(session.PaidByPlayerName)
            ? $"{session.PlayerName} (Paid by {session.PaidByPlayerName})"
            : session.PlayerName;
        this.historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = transactionName,
            Amount     = session.AmountTraded,
            Timestamp  = DateTime.UtcNow,
            GameName   = HigherLowerGameIds.DisplayName,
        });
        this._gameLog.Clear();
        this.config.HigherLowerSession = new HigherLowerTurnState();
        this.config.Save();
        PaymentVerified?.Invoke(session.PlayerName);
        SessionUpdated?.Invoke();
    }

    public void RecordRoll(int value)
    {
        var session = GetActiveSession();
        var turn    = this.config.HigherLowerSession;
        if (session == null || !session.PaymentVerified || turn == null) return;
        if (turn.IsGameOver || turn.IsWinner) return;

        turn.RollLog.Add(value);
        this._gameLog.Add($"You rolled a {value}");

        if (turn.RollLog.Count == 1 || !turn.GuessConfirmed)
        {
            this.config.Save();
            RollAwaitingGuess?.Invoke(value);
            SessionUpdated?.Invoke();
            return;
        }

        var prev = turn.RollLog[^2];
        var curr = value;

        if (curr == prev)
        {
            this._gameLog.Add($"Rolled {curr} again - same number, roll once more!");
            this.config.Save();
            SessionUpdated?.Invoke();
            return;
        }

        var correct = (turn.DetectedGuess == 1 && curr > prev) || (turn.DetectedGuess == -1 && curr < prev);

        if (correct)
        {
            turn.RoundsCorrect++;
            turn.DetectedGuess  = null;
            turn.GuessConfirmed = false;

            var autoWin = this.config.HigherLower.AutoWinCount
                          && turn.RoundsCorrect >= this.config.HigherLower.TargetRounds;
            if (autoWin)
            {
                turn.IsGameOver = true;
                AddToLeaderboard(session, turn, isWinner: true);
                this._gameLog.Add($"{session.PlayerName} wins with {turn.RoundsCorrect} correct round{(turn.RoundsCorrect == 1 ? "" : "s")}!");
                this.config.Save();
                WinDetected?.Invoke(session.PlayerName, turn.RoundsCorrect);
                SessionUpdated?.Invoke();
                return;
            }
            this.config.Save();
            RollAwaitingGuess?.Invoke(value);
            RoundCorrect?.Invoke(session.PlayerName, turn.RoundsCorrect);
            SessionUpdated?.Invoke();
        }
        else
        {
            turn.IsGameOver = true;
            AddToLeaderboard(session, turn, isWinner: false);
            this._gameLog.Add($"{session.PlayerName} made it to {turn.RoundsCorrect} round{(turn.RoundsCorrect == 1 ? "" : "s")}");
            this.config.Save();
            SessionLost?.Invoke(session.PlayerName, turn.RoundsCorrect);
            SessionUpdated?.Invoke();
        }
    }

    public void RegisterGuess(bool isHigher)
    {
        var session = GetActiveSession();
        var turn    = this.config.HigherLowerSession;
        if (session == null || !session.PaymentVerified || turn == null) return;
        if (turn.IsGameOver || turn.IsWinner || turn.GuessConfirmed) return;
        if (turn.RollLog.Count == 0) return;
        this._gameLog.Add($"{session.PlayerName} chose {(isHigher ? "Higher" : "Lower")}");
        turn.DetectedGuess  = isHigher ? 1 : -1;
        turn.GuessConfirmed = true;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void ClearConfirmedGuess()
    {
        var turn = this.config.HigherLowerSession;
        if (turn == null || !turn.GuessConfirmed) return;
        turn.GuessConfirmed = false;
        turn.DetectedGuess  = null;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void EndCurrentTurn()
    {
        var session = GetActiveSession();
        if (session == null) return;
        session.PlayerName       = HigherLowerGameIds.NoPlayerSelectedPlaceholder;
        session.PlayerWorld      = string.Empty;
        session.PlayerSet        = false;
        session.AmountTraded     = 0;
        session.PaidByPlayerName = string.Empty;
        session.PaymentVerified  = false;
        this.config.HigherLowerSession = null;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void ClearLeaderboard()
    {
        this.config.HigherLower.SessionLeaderboard.Clear();
        this.config.HigherLower.SessionTradedTotal = 0L;
        this.config.HigherLower.PlayersPlayed      = 0;
        this.config.HigherLower.SessionFinished    = false;
        this.config.HigherLower.WinnerPayouts.Clear();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public long GetTotalPot() => ComputeTotalPot(this.config);

    public static long ComputeTotalPot(PluginConfiguration config)
    {
        var hl = config.HigherLower;
        return hl.BoostedPot + (hl.SessionTradedTotal * hl.TradesToPotPercent / 100);
    }

    public static long ComputeTradesHeldBack(PluginConfiguration config)
    {
        var hl = config.HigherLower;
        return hl.SessionTradedTotal - (hl.SessionTradedTotal * hl.TradesToPotPercent / 100);
    }

    public int GetWinnerCount() =>
        this.config.HigherLower.SessionLeaderboard.Count(e => e.IsWinner);

    public bool IsCurrentlyLeading(int rounds)
    {
        var board = this.config.HigherLower.SessionLeaderboard;
        if (board.Count == 0) return false;
        var sessMax = board.Max(e => e.RoundsCorrect);
        if (rounds < sessMax) return false;
        if (this.config.HigherLower.AllowMultipleWinners)
            return true;
        return board.Count(e => e.RoundsCorrect >= rounds) <= 1;
    }

    public int GetLeadTarget(int playerRounds)
    {
        var board = this.config.HigherLower.SessionLeaderboard;
        if (board.Count == 0) return playerRounds + 1;
        var sessMax = board.Max(e => e.RoundsCorrect);
        return this.config.HigherLower.AllowMultipleWinners ? sessMax : sessMax + 1;
    }

    public long GetPerWinnerShare()
    {
        var pot     = GetTotalPot();
        var winners = GetWinnerCount();
        return winners > 0 ? pot / winners : pot;
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (!this.sessionService.IsFocused(HigherLowerGameIds.DisplayName)) return;
        var session = GetActiveSession();
        var turn    = this.config.HigherLowerSession;
        if (session == null || !session.PaymentVerified || turn == null) return;
        if (turn.IsGameOver || turn.IsWinner || turn.GuessConfirmed) return;
        if (turn.RollLog.Count == 0) return;
        var kind = message.LogKind;
        if (kind != XivChatType.Party && kind != XivChatType.Alliance) return;
        var senderText = message.Sender?.TextValue ?? string.Empty;
        if (!senderText.Contains(session.PlayerName.Trim(), StringComparison.OrdinalIgnoreCase))
            return;
        var text = message.Message?.TextValue ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return;
        var words = text.Trim().ToLowerInvariant()
            .Split([' ', ',', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (word is "h" or "high" or "higher")
            {
                turn.DetectedGuess = 1;
                this.config.Save();
                SessionUpdated?.Invoke();
                return;
            }
            if (word is "l" or "low" or "lower")
            {
                turn.DetectedGuess = -1;
                this.config.Save();
                SessionUpdated?.Invoke();
                return;
            }
        }
    }

    private void AddToLeaderboard(ActiveSession session, HigherLowerTurnState turn, bool isWinner)
    {
        var displayName = string.IsNullOrEmpty(session.PlayerWorld)
            ? session.PlayerName
            : $"{session.PlayerName}@{session.PlayerWorld}";
        this.config.HigherLower.SessionLeaderboard.Add(new HigherLowerLeaderboardEntry
        {
            PlayerName    = displayName,
            RoundsCorrect = turn.RoundsCorrect,
            IsWinner      = isWinner,
            PlayedAt      = DateTime.UtcNow,
        });
    }

    private void WriteLeaderboardHistory()
    {
        var board = this.config.HigherLower.SessionLeaderboard;
        if (board.Count == 0) return;
        var winners     = GetSessionWinners();
        var winnerNames = winners.Count > 0 ? string.Join(", ", winners.Select(w => w.Name)) : string.Empty;
        this.historyService.AddSession(new SessionRecord
        {
            GameName       = HigherLowerGameIds.DisplayName,
            Winner         = winnerNames,
            BoostedPot     = this.config.HigherLower.BoostedPot,
            AmountInTrades = this.config.HigherLower.SessionTradedTotal,
            KeptFromTrades = ComputeTradesHeldBack(this.config),
            TotalPot       = GetTotalPot(),
            PlayersPlayed  = this.config.HigherLower.PlayersPlayed,
            RoundsPlayed   = board.Max(e => e.RoundsCorrect),
            Timestamp      = DateTime.UtcNow,
        });
    }

    private static bool TradeNameMatchesSession(ActiveSession session, string tradeName)
    {
        if (string.IsNullOrEmpty(session.PlayerName)) return false;
        return session.PlayerName.Equals(tradeName, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        this.chatGui.ChatMessage -= OnChatMessage;
    }
}
