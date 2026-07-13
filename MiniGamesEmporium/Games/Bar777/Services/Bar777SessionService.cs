using System;
using System.Collections.Generic;
using System.Linq;

using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Utility;

/// <summary>Manages the full lifecycle of a BAR 777 game session.</summary>

namespace MiniGamesEmporium.Games.Bar777.Services;
public sealed class Bar777SessionService
{
    private readonly PluginConfiguration config;
    private readonly HistoryService historyService;

    public event Action? SessionUpdated;
    public event Action<string, int>? WinDetected;
    public event Action<string, int>? HalfwayReached;
    public event Action<string>? SessionLost;
    public event Action<string>? PaymentVerified;
    public event Action<string>? PlayerEnqueued;

    public bool IsQueuePaused { get; private set; }

    public Bar777SessionService(PluginConfiguration config, HistoryService historyService)
    {
        this.config = config;
        this.historyService = historyService;
    }

    public IReadOnlyList<string> Queue => this.config.QueuedPlayers;

    public void TryEnqueuePlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;
        if (Bar777GameIds.IsAnyPlaceholder(playerName)) return;
        var (incomingName, _) = PlayerInfoService.SplitNameAndWorld(playerName.Trim());
        if (this.config.QueuedPlayers.Any(q => PlayerInfoService.SplitNameAndWorld(q).Name.Equals(incomingName, StringComparison.OrdinalIgnoreCase)))
            return;
        var wasEmpty = this.config.QueuedPlayers.Count == 0;
        this.config.QueuedPlayers.Add(playerName);
        PlayerEnqueued?.Invoke(playerName);
        if (wasEmpty && TryPromoteBar777WaitingIdleToFirstQueuedPlayer(playerName.Trim()))
            return;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void RemoveFromQueue(int index)
    {
        if (index < 0 || index >= this.config.QueuedPlayers.Count) return;
        this.config.QueuedPlayers.RemoveAt(index);
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void DequeueFirst()
    {
        if (this.config.QueuedPlayers.Count == 0) return;
        this.config.QueuedPlayers.RemoveAt(0);
        this.config.ActiveSession = null;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void StartSession(string gameName, string playerName)
    {
        if (this.config.ActiveSession != null) return;
        if (string.IsNullOrWhiteSpace(playerName)) return;
        var (name, world) = PlayerInfoService.SplitNameAndWorld(playerName.Trim());
        IsQueuePaused = false;
        this.config.ActiveSession = new ActiveSession
        {
            GameName = gameName,
            PlayerName = name,
            PlayerWorld = world,
            RollsAllowed = this.config.Bar777.MaxRolls,
            RollsUsed = 0,
            PaymentVerified = false,
            WinTriggered = false,
            AmountTraded = 0,
            RollLog = [],
            StartedAt = DateTime.UtcNow,
        };
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void VerifyPayment(string playerName, int amount, string playerWorld = "")
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return;
        if (session.PaymentVerified)
            return;
        var isPlaceholder = !this.config.Bar777.UseQueue && Bar777GameIds.IsAnyPlaceholder(session.PlayerName);
        var buyerBaseName = PlayerInfoService.StripWorld(session.PaidByPlayerName);
        var isBuyerTrade = !string.IsNullOrEmpty(session.PaidByPlayerName)
            && buyerBaseName.Equals(playerName, StringComparison.OrdinalIgnoreCase);
        if (!isPlaceholder && !isBuyerTrade && !TradeNameMatchesSession(session, playerName))
            return;
        if (isPlaceholder && !isBuyerTrade)
        {
            session.PlayerName = playerName;
            session.PlayerWorld = string.IsNullOrEmpty(playerWorld) ? session.PlayerWorld : playerWorld;
            session.PlayerSet = true;
        }
        session.AmountTraded += amount;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void StartGameWithRolls(int rollCount)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (session.PaymentVerified) return;
        if (rollCount < 1) return;
        session.RollsAllowed = Math.Min(rollCount, this.config.Bar777.MaxRolls);
        session.PaymentVerified = true;
        this.config.Bar777.PlayersPlayed++;
        this.config.Bar777.SessionTradedTotal += session.AmountTraded;
        var transactionName = !string.IsNullOrEmpty(session.PaidByPlayerName)
            ? $"{session.PlayerName} (Paid by {session.PaidByPlayerName})"
            : session.PlayerName;
        this.historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = transactionName,
            Amount = session.AmountTraded,
            Timestamp = DateTime.UtcNow,
            GameName = this.config.Bar777.CustomName,
        });
        this.config.Save();
        PaymentVerified?.Invoke(session.PlayerName);
        SessionUpdated?.Invoke();
    }

    public bool TryCatchPaymentRoll(string senderName, int rollValue)
    {
        if (!this.config.Bar777.AutoCatchRoll) return false;
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return false;
        if (session.PaymentVerified) return false;
        if (session.AmountTraded <= 0) return false;
        if (string.IsNullOrEmpty(senderName)) return false;
        if (!senderName.Equals(session.PlayerName, StringComparison.OrdinalIgnoreCase)) return false;
        var costPerRoll = this.config.Bar777.CostPerRoll;
        if (costPerRoll <= 0) return false;
        var rollCount = session.AmountTraded / costPerRoll;
        if (rollCount < 1) return false;
        StartGameWithRolls(rollCount);
        RecordRoll(rollValue);
        return true;
    }

    public void RecordRoll(int rollValue)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName) || !session.PaymentVerified) return;
        if (session.WinTriggered || session.RollsUsed >= session.RollsAllowed) return;
        session.RollsUsed++;
        session.RollLog ??= [];
        session.RollLog.Add(rollValue);
        if (rollValue == this.config.Bar777.WinNumber && !session.WinTriggered)
        {
            session.WinTriggered = true;
            this.config.Save();
            WinDetected?.Invoke(session.PlayerName, rollValue);
            SessionUpdated?.Invoke();
            return;
        }
        var halfway = session.RollsAllowed / 2;
        if (session.RollsUsed == halfway)
            HalfwayReached?.Invoke(session.PlayerName, session.RollsAllowed - session.RollsUsed);
        if (session.RollsUsed >= session.RollsAllowed)
        {
            this.config.Save();
            SessionLost?.Invoke(session.PlayerName);
            SessionUpdated?.Invoke();
            return;
        }
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void RemoveRoll(int index)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        var log = session.RollLog;
        if (log == null || index < 0 || index >= log.Count) return;
        var removedValue = log[index];
        log.RemoveAt(index);
        session.RollsUsed = Math.Max(0, session.RollsUsed - 1);
        if (session.WinTriggered && removedValue == this.config.Bar777.WinNumber)
            session.WinTriggered = log.Contains(this.config.Bar777.WinNumber);
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void PauseQueue()
    {
        IsQueuePaused = true;
        SessionUpdated?.Invoke();
    }

    public void ResumeQueue()
    {
        IsQueuePaused = false;
        SessionUpdated?.Invoke();
    }

    public void TryRecordWinnerPayout(string partnerName, long amountSent)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (!session.WinTriggered) return;
        if (!session.PlayerName.Equals(partnerName, StringComparison.OrdinalIgnoreCase)) return;
        session.WinnerPayoutGil += amountSent;
        session.PayoutTransactionId = PayoutTransactionRecorder.Record(this.historyService, this.config.Bar777.CustomName, session.PlayerName, session.WinnerPayoutGil, session.PayoutTransactionId);
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public bool IsSessionActive() => this.config.ActiveSession != null;

    public ActiveSession? GetActiveSession() => this.config.ActiveSession;

    public string GetBuyer()
    {
        return this.config.ActiveSession?.PaidByPlayerName ?? string.Empty;
    }

    public void SetBuyer(string buyerName)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (string.IsNullOrWhiteSpace(buyerName)) return;
        session.PaidByPlayerName = buyerName.Trim();
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void ClearBuyer()
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        session.PaidByPlayerName = string.Empty;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void LockWalkInPlayer(string charName, string worldName)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (this.config.Bar777.UseQueue) return;
        var trimmedName = charName.Trim();
        var trimmedWorld = worldName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName)) return;
        session.PlayerName = trimmedName;
        session.PlayerWorld = trimmedWorld;
        session.PlayerSet = true;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void UnlockWalkInPlayer()
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        if (this.config.Bar777.UseQueue) return;
        session.PlayerName = Bar777GameIds.WalkInPlayerPlaceholder;
        session.PlayerWorld = string.Empty;
        session.PlayerSet = false;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void CancelSession()
    {
        IsQueuePaused = false;
        RecordSessionHistory();
        ClearGameStats();
        this.config.QueuedPlayers.Clear();
        this.config.ActiveSession = null;
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void ClearGameStats()
    {
        this.config.Bar777.PlayersPlayed = 0;
        this.config.Bar777.BoostedPot = 0;
        this.config.Bar777.SessionTradedTotal = 0;
    }

    public void EndWalkInAndReset()
    {
        this.config.ActiveSession = new ActiveSession
        {
            GameName = Bar777GameIds.DisplayName,
            PlayerName = Bar777GameIds.WalkInPlayerPlaceholder,
            RollsAllowed = this.config.Bar777.MaxRolls,
            AmountTraded = 0,
            RollLog = [],
            StartedAt = DateTime.UtcNow,
        };
        this.config.Save();
        SessionUpdated?.Invoke();
    }

    public void EndQueuePlayerAndProcessNext()
    {
        RemoveCurrentBar777FromWaitlistAndStartNext();
    }

    public void EndSessionWalkIn()
    {
        RecordSessionHistory();
        ClearGameStats();
        EndWalkInAndReset();
    }

    public void EndSessionQueue()
    {
        RecordSessionHistory();
        ClearGameStats();
        RemoveCurrentBar777FromWaitlistAndStartNext();
    }

    public void RemoveCurrentBar777FromWaitlistAndStartNext()
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return;
        var name = session.PlayerName.Trim();
        if (Bar777GameIds.IsAnyPlaceholder(name))
            return;
        this.config.ActiveSession = null;
        if (!string.IsNullOrWhiteSpace(name))
            RemoveQueuedNamesMatching(name);
        if (this.config.QueuedPlayers.Count > 0)
        {
            StartSession(Bar777GameIds.DisplayName, this.config.QueuedPlayers[0]);
            return;
        }
        StartSession(Bar777GameIds.DisplayName, Bar777GameIds.WaitingPlayerPlaceholder);
    }

    public void SendCurrentBar777ToBackOfWaitlistAndStartNext()
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return;
        var name = session.PlayerName.Trim();
        if (string.IsNullOrWhiteSpace(name) || Bar777GameIds.IsAnyPlaceholder(name))
            return;
        this.config.ActiveSession = null;
        var fullName = string.IsNullOrEmpty(session.PlayerWorld) ? name : $"{name}@{session.PlayerWorld}";
        RemoveQueuedNamesMatching(name);
        this.config.QueuedPlayers.Add(fullName);
        if (this.config.QueuedPlayers.Count > 0)
        {
            StartSession(Bar777GameIds.DisplayName, this.config.QueuedPlayers[0]);
            return;
        }
        StartSession(Bar777GameIds.DisplayName, Bar777GameIds.WaitingPlayerPlaceholder);
    }

    public bool IsActive() =>
        this.config.ActiveSession != null && Bar777GameIds.Matches(this.config.ActiveSession.GameName);

    private bool TryPromoteBar777WaitingIdleToFirstQueuedPlayer(string trimmedName)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return false;
        if (!Bar777GameIds.IsWaitingPlaceholder(session.PlayerName))
            return false;
        var (name, world) = PlayerInfoService.SplitNameAndWorld(trimmedName);
        session.PlayerName = name;
        session.PlayerWorld = world;
        session.PlayerSet = false;
        session.RollsAllowed = this.config.Bar777.MaxRolls;
        session.RollsUsed = 0;
        session.RollLog = [];
        session.PaymentVerified = false;
        session.WinTriggered = false;
        session.AmountTraded = 0;
        session.PaidByPlayerName = string.Empty;
        session.StartedAt = DateTime.UtcNow;
        this.config.Save();
        SessionUpdated?.Invoke();
        return true;
    }

    public long ComputeTotalPot() => ComputeTotalPot(this.config);

    public static long ComputeTotalPot(PluginConfiguration config)
    {
        var bar = config.Bar777;
        return bar.BoostedPot + (bar.SessionTradedTotal * bar.TradesToPotPercent / 100);
    }

    public static long ComputeTradesHeldBack(PluginConfiguration config)
    {
        var bar = config.Bar777;
        return bar.SessionTradedTotal - (bar.SessionTradedTotal * bar.TradesToPotPercent / 100);
    }

    private void RecordSessionHistory()
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        var amountInTrades = this.config.Bar777.SessionTradedTotal;
        var totalPot = ComputeTotalPot(this.config);
        this.historyService.AddSession(new SessionRecord
        {
            GameName = session.GameName,
            Winner = session.WinTriggered && !Bar777GameIds.IsAnyPlaceholder(session.PlayerName) ? session.PlayerName : string.Empty,
            BoostedPot = this.config.Bar777.BoostedPot,
            AmountInTrades = amountInTrades,
            KeptFromTrades = ComputeTradesHeldBack(this.config),
            TotalPot = totalPot,
            PlayersPlayed = this.config.Bar777.PlayersPlayed,
            Timestamp = DateTime.UtcNow,
        });
    }

    private void RemoveQueuedNamesMatching(string name)
    {
        var q = this.config.QueuedPlayers;
        for (var i = q.Count - 1; i >= 0; i--)
        {
            var (qName, _) = PlayerInfoService.SplitNameAndWorld(q[i]);
            if (qName.Equals(name, StringComparison.OrdinalIgnoreCase))
                q.RemoveAt(i);
        }
    }

    private static bool TradeNameMatchesSession(ActiveSession session, string tradeName)
    {
        if (session.PlayerName.Equals(tradeName, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.IsNullOrEmpty(session.PlayerWorld))
            return false;
        return (session.PlayerName + session.PlayerWorld).Equals(tradeName, StringComparison.OrdinalIgnoreCase);
    }
}
