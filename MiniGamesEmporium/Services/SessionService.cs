using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Games.Bar777.State;
using MiniGamesEmporium.State;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Manages the full lifecycle of a BAR 777 game session, including queue management, session start and end, payment verification, roll recording, and writing completed sessions to history.</summary>

namespace MiniGamesEmporium.Services;
public sealed class SessionService
{
    private readonly PluginConfiguration config;
    public event Action? SessionUpdated;
    public event Action<string, int>? WinDetected;
    public event Action<string, int>? HalfwayReached;
    public event Action<string>? SessionLost;
    public event Action<string>? PaymentVerified;
    public event Action<string>? PlayerEnqueued;
    public SessionService(PluginConfiguration config)
    {
        this.config = config;
    }
    public IReadOnlyList<string> Queue => this.config.QueuedPlayers;
    public void TryEnqueuePlayer(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;
        if (Bar777GameIds.IsAnyPlaceholder(playerName)) return;
        var (incomingName, _) = ParseQueuedName(playerName.Trim());
        if (this.config.QueuedPlayers.Any(q => ParseQueuedName(q).Name.Equals(incomingName, StringComparison.OrdinalIgnoreCase)))
            return;
        var wasEmpty = this.config.QueuedPlayers.Count == 0;
        this.config.QueuedPlayers.Add(playerName);
        PlayerEnqueued?.Invoke(playerName);
        if (wasEmpty && TryPromoteBar777WaitingIdleToFirstQueuedPlayer(playerName.Trim()))
            return;
        this.config.Save();
        SessionUpdated?.Invoke();
    }
    private bool TryPromoteBar777WaitingIdleToFirstQueuedPlayer(string trimmedName)
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return false;
        if (!Bar777GameIds.IsWaitingPlaceholder(session.PlayerName))
            return false;
        var (name, world) = ParseQueuedName(trimmedName);
        session.PlayerName = name;
        session.PlayerWorld = world;
        session.PlayerSet = false;
        session.RollsAllowed = this.config.Bar777.MaxRolls;
        session.RollsUsed = 0;
        session.RollLog = [];
        session.PaymentVerified = false;
        session.WinTriggered = false;
        session.AmountTraded = 0;
        session.StartedAt = DateTime.UtcNow;
        this.config.Save();
        SessionUpdated?.Invoke();
        return true;
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
        var (name, world) = ParseQueuedName(playerName.Trim());
        IsPaused = false;
        this.config.ActiveSession = new ActiveSessionState
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
        if (!isPlaceholder && !TradeNameMatchesSession(session, playerName))
            return;
        if (isPlaceholder)
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
        this.config.Transactions.Add(new TransactionRecord
        {
            PlayerName = session.PlayerName,
            Amount = session.AmountTraded,
            Timestamp = DateTime.UtcNow,
            GameName = session.GameName,
        });
        this.config.Save();
        PaymentVerified?.Invoke(session.PlayerName);
        SessionUpdated?.Invoke();
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
    public bool IsPaused { get; private set; }

    public void PauseSession()
    {
        IsPaused = true;
        SessionUpdated?.Invoke();
    }

    public void ResumeSession()
    {
        IsPaused = false;
        SessionUpdated?.Invoke();
    }

    public ActiveSessionState? GetActiveSession() => this.config.ActiveSession;
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
        IsPaused = false;
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

    public void PruneOldTransactions()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        this.config.Transactions.RemoveAll(t => t.Timestamp < cutoff);
        this.config.Save();
    }
    public void EndWalkInAndReset()
    {
        IsPaused = false;
        this.config.ActiveSession = new ActiveSessionState
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
    private void RecordSessionHistory()
    {
        var session = this.config.ActiveSession;
        if (session == null || !Bar777GameIds.Matches(session.GameName)) return;
        var amountInTrades = this.config.Bar777.SessionTradedTotal;
        var totalPot = this.config.Bar777.BoostedPot + amountInTrades;
        this.config.SessionHistory.Add(new SessionRecord
        {
            GameName = session.GameName,
            Winner = session.WinTriggered && !Bar777GameIds.IsAnyPlaceholder(session.PlayerName) ? session.PlayerName : string.Empty,
            BoostedPot = this.config.Bar777.BoostedPot,
            AmountInTrades = amountInTrades,
            TotalPot = totalPot,
            PlayersPlayed = this.config.Bar777.PlayersPlayed,
            Timestamp = DateTime.UtcNow,
        });
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
    private void RemoveQueuedNamesMatching(string name)
    {
        var q = this.config.QueuedPlayers;
        for (var i = q.Count - 1; i >= 0; i--)
        {
            var (qName, _) = ParseQueuedName(q[i]);
            if (qName.Equals(name, StringComparison.OrdinalIgnoreCase))
                q.RemoveAt(i);
        }
    }
    private static (string Name, string World) ParseQueuedName(string raw)
    {
        var at = raw.IndexOf('@');
        return at < 0 ? (raw, string.Empty) : (raw[..at].Trim(), raw[(at + 1)..].Trim());
    }
    private static bool TradeNameMatchesSession(ActiveSessionState session, string tradeName)
    {
        if (session.PlayerName.Equals(tradeName, StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.IsNullOrEmpty(session.PlayerWorld))
            return false;
        return (session.PlayerName + session.PlayerWorld).Equals(tradeName, StringComparison.OrdinalIgnoreCase);
    }
}
