using System;
using System.Collections.Generic;
using System.Linq;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.CoinCollector.Models;
using MiniGamesEmporium.Games.CoinCollector.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;

/// <summary>Manages the full lifecycle of a Coin Collector game session.</summary>

namespace MiniGamesEmporium.Games.CoinCollector.Services;
public sealed class CoinCollectorService
{
    private const int DefaultDiceMax = 999;

    private readonly PluginConfiguration config;
    private readonly HistoryService historyService;
    private readonly List<string> _gameLog = [];

    public event Action? SessionUpdated;
    public event Action<string, int>? WinDetected;
    public event Action<string, int>? SessionLost;
    public event Action<int, int>? RollAwaitingNext;

    public CoinCollectorService(PluginConfiguration config, HistoryService historyService)
    {
        this.config         = config;
        this.historyService = historyService;
    }

    public bool IsSessionActive() => this.config.CoinCollectorActiveSession != null;

    public ActiveSession? GetActiveSession() => this.config.CoinCollectorActiveSession;

    public CoinCollectorTurnState? GetActiveTurn() => this.config.CoinCollectorSession;

    public IReadOnlyList<string> GetGameLog() => this._gameLog;

    public void StartSession()
    {
        if (this.config.CoinCollectorActiveSession != null) return;
        this.config.CoinCollectorActiveSession = new ActiveSession
        {
            GameName        = CoinCollectorGameIds.DisplayName,
            PlayerName      = CoinCollectorGameIds.NoPlayerSelectedPlaceholder,
            PlayerWorld     = string.Empty,
            PlayerSet       = false,
            PaymentVerified = false,
            AmountTraded    = 0,
            RollLog         = [],
            StartedAt       = DateTime.UtcNow,
        };
        this.config.CoinCollectorSession          = null;
        this.config.CoinCollector.SessionFinished = false;
        this.config.CoinCollector.WinnerPayouts.Clear();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void CancelSession()
    {
        if (!IsSessionActive()) return;
        WriteLeaderboardHistory();
        this.config.CoinCollector.SessionLeaderboard.Clear();
        this.config.CoinCollector.SessionTradedTotal = 0L;
        this.config.CoinCollector.PlayersPlayed      = 0;
        this.config.CoinCollector.SessionFinished    = false;
        this.config.CoinCollector.WinnerPayouts.Clear();
        this.config.CoinCollectorActiveSession = null;
        this.config.CoinCollectorSession       = null;
        this._gameLog.Clear();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public bool IsSessionFinished() => IsSessionActive() && this.config.CoinCollector.SessionFinished;

    public void FinishSession()
    {
        if (!IsSessionActive()) return;
        var winners = GetSessionWinners();
        this.config.CoinCollector.WinnerPayouts = winners
            .Select(w => new CoinCollectorWinnerPayout { PlayerName = w.Name, PaidGil = 0L })
            .ToList();
        this.config.CoinCollector.SessionFinished = true;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void ResumeFinishedSession()
    {
        if (!IsSessionFinished()) return;
        this.config.CoinCollector.SessionFinished = false;
        this.config.CoinCollector.WinnerPayouts.Clear();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public List<(string Name, int Coins)> GetSessionWinners()
    {
        var board = this.config.CoinCollector.SessionLeaderboard;
        if (board.Count == 0) return [];

        var groups = new Dictionary<string, (int Best, DateTime FirstBestAt, bool HasWin)>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in board)
        {
            if (!groups.TryGetValue(e.PlayerName, out var g))
            {
                groups[e.PlayerName] = (e.Coins, e.PlayedAt, e.IsWinner);
                continue;
            }
            var firstBestAt = e.Coins > g.Best ? e.PlayedAt
                            : e.Coins == g.Best && e.PlayedAt < g.FirstBestAt ? e.PlayedAt
                            : g.FirstBestAt;
            groups[e.PlayerName] = (Math.Max(g.Best, e.Coins), firstBestAt, g.HasWin || e.IsWinner);
        }

        if (this.config.CoinCollector.AllowMultipleWinners)
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
        var count = this.config.CoinCollector.WinnerPayouts.Count;
        return count > 0 ? pot / count : pot;
    }

    public long GetWinnerPaid(string displayName) =>
        this.config.CoinCollector.WinnerPayouts
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
        session.PlayerName       = CoinCollectorGameIds.NoPlayerSelectedPlaceholder;
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
        var payout = this.config.CoinCollector.WinnerPayouts
            .FirstOrDefault(w => PlayerInfoService.StripWorld(w.PlayerName).Equals(partnerName, StringComparison.OrdinalIgnoreCase));
        if (payout == null) return;
        payout.PaidGil += amountSent;
        payout.PayoutTransactionId = PayoutTransactionRecorder.Record(this.historyService, CoinCollectorGameIds.DisplayName, payout.PlayerName, payout.PaidGil, payout.PayoutTransactionId);
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void StartGame()
    {
        var session = GetActiveSession();
        if (session == null || session.PaymentVerified) return;
        session.PaymentVerified = true;
        this.config.CoinCollector.PlayersPlayed++;
        this.config.CoinCollector.SessionTradedTotal += session.AmountTraded;
        var transactionName = !string.IsNullOrEmpty(session.PaidByPlayerName)
            ? $"{session.PlayerName} (Paid by {session.PaidByPlayerName})"
            : session.PlayerName;
        this.historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = transactionName,
            Amount     = session.AmountTraded,
            Timestamp  = DateTime.UtcNow,
            GameName   = CoinCollectorGameIds.DisplayName,
        });
        this._gameLog.Clear();
        this.config.CoinCollectorSession = new CoinCollectorTurnState();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void RecordRoll(int rollValue, int rollMax)
    {
        var session = GetActiveSession();
        var turn    = this.config.CoinCollectorSession;
        if (session == null || !session.PaymentVerified || turn == null) return;
        if (turn.IsGameOver || turn.IsWinner) return;

        var isSeedRoll = turn.CurrentRollMax == 0;
        if (!TryResolveRollMax(turn, rollMax, out var effectiveMax)) return;

        turn.RollLog.Add(rollValue);
        turn.RollMaxLog.Add(effectiveMax);

        if (rollValue == 1)
        {
            turn.IsGameOver = true;
            this._gameLog.Add($"Rolled a 1 out of {effectiveMax} - turn over.");
            AddToLeaderboard(session, turn, isWinner: false);
            this._gameLog.Add($"{session.PlayerName} collected {turn.CoinsCollected} coin{(turn.CoinsCollected == 1 ? "" : "s")}");
            this.config.Save();
            SessionLost?.Invoke(session.PlayerName, turn.CoinsCollected);
            SessionUpdated?.Invoke();
            return;
        }

        turn.CurrentRollMax = rollValue;
        turn.CoinsCollected++;

        this._gameLog.Add(isSeedRoll
            ? $"Starting number is {rollValue} - coin collected. Total: {turn.CoinsCollected}"
            : $"Rolled {rollValue} out of {effectiveMax} - coin collected. Total: {turn.CoinsCollected}");

        var autoWin = this.config.CoinCollector.AutoWinCount
                      && turn.CoinsCollected >= this.config.CoinCollector.TargetCoins;
        if (autoWin)
        {
            turn.IsGameOver = true;
            AddToLeaderboard(session, turn, isWinner: true);
            this._gameLog.Add($"{session.PlayerName} wins with {turn.CoinsCollected} coin{(turn.CoinsCollected == 1 ? "" : "s")}");
            this.config.Save();
            WinDetected?.Invoke(session.PlayerName, turn.CoinsCollected);
            SessionUpdated?.Invoke();
            return;
        }

        this.config.Save();
        RollAwaitingNext?.Invoke(rollValue, turn.CoinsCollected);
        SessionUpdated?.Invoke();
    }

    private bool TryResolveRollMax(CoinCollectorTurnState turn, int rollMax, out int effectiveMax)
    {
        if (turn.CurrentRollMax != 0)
        {
            effectiveMax = turn.CurrentRollMax;
            return rollMax == turn.CurrentRollMax;
        }
        var configured = GetStartingRollMax();
        effectiveMax = configured;
        return configured == DefaultDiceMax ? rollMax == 0 || rollMax == DefaultDiceMax : rollMax == configured;
    }

    private int GetStartingRollMax()
    {
        var configured = this.config.CoinCollector.StartingRollMax;
        return configured <= 1 ? DefaultDiceMax : configured;
    }

    public int GetNextRollCommandMax()
    {
        var turn = this.config.CoinCollectorSession;
        if (turn == null || turn.IsGameOver || turn.IsWinner) return 0;
        if (turn.CurrentRollMax != 0) return turn.CurrentRollMax;
        var configured = GetStartingRollMax();
        return configured == DefaultDiceMax ? 0 : configured;
    }

    public void EndCurrentTurn()
    {
        var session = GetActiveSession();
        if (session == null) return;
        session.PlayerName       = CoinCollectorGameIds.NoPlayerSelectedPlaceholder;
        session.PlayerWorld      = string.Empty;
        session.PlayerSet        = false;
        session.AmountTraded     = 0;
        session.PaidByPlayerName = string.Empty;
        session.PaymentVerified  = false;
        this.config.CoinCollectorSession = null;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void ClearLeaderboard()
    {
        this.config.CoinCollector.SessionLeaderboard.Clear();
        this.config.CoinCollector.SessionTradedTotal = 0L;
        this.config.CoinCollector.PlayersPlayed      = 0;
        this.config.CoinCollector.SessionFinished    = false;
        this.config.CoinCollector.WinnerPayouts.Clear();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public long GetTotalPot() => ComputeTotalPot(this.config);

    public static long ComputeTotalPot(PluginConfiguration config)
    {
        var cc = config.CoinCollector;
        return cc.BoostedPot + (cc.SessionTradedTotal * cc.TradesToPotPercent / 100);
    }

    public static long ComputeTradesHeldBack(PluginConfiguration config)
    {
        var cc = config.CoinCollector;
        return cc.SessionTradedTotal - (cc.SessionTradedTotal * cc.TradesToPotPercent / 100);
    }

    public int GetWinnerCount() =>
        this.config.CoinCollector.SessionLeaderboard.Count(e => e.IsWinner);

    public bool IsCurrentlyLeading(int coins)
    {
        var board = this.config.CoinCollector.SessionLeaderboard;
        if (board.Count == 0) return false;
        var sessMax = board.Max(e => e.Coins);
        if (coins < sessMax) return false;
        if (this.config.CoinCollector.AllowMultipleWinners)
            return true;
        return board.Count(e => e.Coins >= coins) <= 1;
    }

    public int GetLeadTarget(int playerCoins)
    {
        var board = this.config.CoinCollector.SessionLeaderboard;
        if (board.Count == 0) return playerCoins + 1;
        var sessMax = board.Max(e => e.Coins);
        return this.config.CoinCollector.AllowMultipleWinners ? sessMax : sessMax + 1;
    }

    public long GetPerWinnerShare()
    {
        var pot     = GetTotalPot();
        var winners = GetWinnerCount();
        return winners > 0 ? pot / winners : pot;
    }

    private void AddToLeaderboard(ActiveSession session, CoinCollectorTurnState turn, bool isWinner)
    {
        var displayName = string.IsNullOrEmpty(session.PlayerWorld)
            ? session.PlayerName
            : $"{session.PlayerName}@{session.PlayerWorld}";
        this.config.CoinCollector.SessionLeaderboard.Add(new CoinCollectorLeaderboardEntry
        {
            PlayerName = displayName,
            Coins      = turn.CoinsCollected,
            IsWinner   = isWinner,
            PlayedAt   = DateTime.UtcNow,
        });
    }

    private void WriteLeaderboardHistory()
    {
        var board = this.config.CoinCollector.SessionLeaderboard;
        if (board.Count == 0) return;
        var winners     = GetSessionWinners();
        var winnerNames = winners.Count > 0 ? string.Join(", ", winners.Select(w => w.Name)) : string.Empty;
        this.historyService.AddSession(new SessionRecord
        {
            GameName       = CoinCollectorGameIds.DisplayName,
            Winner         = winnerNames,
            BoostedPot     = this.config.CoinCollector.BoostedPot,
            AmountInTrades = this.config.CoinCollector.SessionTradedTotal,
            KeptFromTrades = ComputeTradesHeldBack(this.config),
            TotalPot       = GetTotalPot(),
            PlayersPlayed  = this.config.CoinCollector.PlayersPlayed,
            RoundsPlayed   = board.Max(e => e.Coins),
            Timestamp      = DateTime.UtcNow,
        });
    }

    private static bool TradeNameMatchesSession(ActiveSession session, string tradeName)
    {
        if (string.IsNullOrEmpty(session.PlayerName)) return false;
        return session.PlayerName.Equals(tradeName, StringComparison.OrdinalIgnoreCase);
    }
}
