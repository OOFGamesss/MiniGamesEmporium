using System;
using System.Collections.Generic;
using System.Linq;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Models;
using MiniGamesEmporium.Games.Raffle.State;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;

/// <summary>Manages the full lifecycle of a Raffle session, its ticket pool, credit, closing time and draw.</summary>

namespace MiniGamesEmporium.Games.Raffle.Services;
public sealed class RaffleService
{
    private static readonly Random Rng = new();

    private readonly PluginConfiguration config;
    private readonly HistoryService historyService;

    public event Action? SessionUpdated;
    public event Action<string, int, long>? WinnerDrawn;
    public event Action<string>? TicketsAutoGranted;

    public RaffleService(PluginConfiguration config, HistoryService historyService)
    {
        this.config         = config;
        this.historyService = historyService;
    }

    public bool IsSessionActive() => this.config.RaffleSession != null;
    public RaffleState? GetState() => this.config.RaffleSession;

    public void StartSession()
    {
        if (this.config.RaffleSession != null) return;
        var cfg = this.config.Raffle;
        var state = new RaffleState
        {
            TicketCostAtStart          = Math.Max(0, cfg.TicketCost),
            BoostedPotAtStart          = Math.Max(0, cfg.BoostedPot),
            MaxTicketsPerPlayerAtStart = Math.Clamp(cfg.MaxTicketsPerPlayer, 1, RaffleGameIds.MaxTicketPoolSize),
            TradesToPotPercentAtStart  = Math.Clamp(cfg.TradesToPotPercent, 0, 100),
            ShuffleModeAtStart         = cfg.ShuffleTicketNumbers,
            StartedAt                  = DateTime.UtcNow,
            CloseHour                  = cfg.CloseHour,
            CloseMinute                = cfg.CloseMinute,
        };
        if (state.CloseHour >= 0)
            state.CloseAtUtc = RaffleTimeUtil.NextOccurrence(state.CloseHour, state.CloseMinute);
        this.config.RaffleSession = state;
        Save();
        SessionUpdated?.Invoke();
    }

    public void StopSession()
    {
        RecordSessionHistory();
        this.config.RaffleSession = null;
        Save();
        SessionUpdated?.Invoke();
    }

    private void RecordSessionHistory()
    {
        var state = this.config.RaffleSession;
        if (state == null) return;
        var amountInTrades = ComputeTradesTotal();
        this.historyService.AddSession(new SessionRecord
        {
            GameName       = RaffleGameIds.DisplayName,
            Winner         = state.WinnerName == null ? string.Empty : PlayerInfoService.StripWorld(state.WinnerName),
            BoostedPot     = state.BoostedPotAtStart,
            AmountInTrades = amountInTrades,
            KeptFromTrades = ComputeTradesHeldBack(),
            TotalPot       = ComputeTotalPot(),
            PlayersPlayed  = ComputePlayersWithTickets(),
            Timestamp      = DateTime.UtcNow,
        });
    }

    public void AddPlayer(string playerEntry)
    {
        var state = this.config.RaffleSession;
        if (state == null || string.IsNullOrWhiteSpace(playerEntry)) return;
        var name = PlayerInfoService.StripWorld(playerEntry.Trim());
        if (state.Entries.Any(e => PlayerInfoService.StripWorld(e.PlayerName).Equals(name, StringComparison.OrdinalIgnoreCase)))
            return;
        state.Entries.Add(new RaffleEntry
        {
            PlayerName = playerEntry.Trim(),
            Verified   = true,
        });
        Save();
        SessionUpdated?.Invoke();
    }

    public void RemovePlayer(int index)
    {
        var state = this.config.RaffleSession;
        if (state == null || index < 0 || index >= state.Entries.Count) return;
        var name = PlayerInfoService.StripWorld(state.Entries[index].PlayerName);
        state.Blocks.RemoveAll(b => NamesMatch(b.Owner, name));
        state.Entries.RemoveAt(index);
        Save();
        SessionUpdated?.Invoke();
    }

    public string GetPlayerBuyer(string playerEntry) => FindEntry(playerEntry)?.Buyer ?? string.Empty;

    public void SetPlayerBuyer(string playerEntry, string buyerName)
    {
        var entry = FindEntry(playerEntry);
        if (entry == null || string.IsNullOrWhiteSpace(buyerName)) return;
        entry.Buyer = buyerName.Trim();
        Save();
        SessionUpdated?.Invoke();
    }

    public void ClearPlayerBuyer(string playerEntry)
    {
        var entry = FindEntry(playerEntry);
        if (entry == null || string.IsNullOrEmpty(entry.Buyer)) return;
        entry.Buyer = string.Empty;
        Save();
        SessionUpdated?.Invoke();
    }

    public void AddTicketsManual(string playerEntry, int count)
    {
        var state = this.config.RaffleSession;
        if (state == null || count <= 0) return;
        var entry = FindEntry(playerEntry);
        if (entry == null) return;
        var granted = Allocate(state, entry.PlayerName, count);
        if (granted > 0)
        {
            this.historyService.AddTransaction(new TransactionRecord
            {
                PlayerName = PlayerInfoService.StripWorld(entry.PlayerName),
                Amount     = 0,
                Timestamp  = DateTime.UtcNow,
                GameName   = RaffleGameIds.DisplayName,
            });
            Save();
            SessionUpdated?.Invoke();
        }
    }

    public void RemoveLastTicketBlock(string playerEntry)
    {
        var state = this.config.RaffleSession;
        if (state == null) return;
        var name  = PlayerInfoService.StripWorld(playerEntry);
        var block = state.Blocks.LastOrDefault(b => NamesMatch(b.Owner, name));
        if (block == null) return;
        state.Blocks.Remove(block);
        Save();
        SessionUpdated?.Invoke();
    }

    public void TryAutoAddTickets(string tradePartner, long gilReceived, string world)
    {
        var state = this.config.RaffleSession;
        if (state == null || gilReceived <= 0) return;
        var cost = state.TicketCostAtStart;
        if (cost <= 0) return;

        var entry = ResolvePayingEntry(state, tradePartner);
        if (entry == null)
        {
            var fullName = string.IsNullOrEmpty(world) ? tradePartner : $"{tradePartner}@{world}";
            entry = new RaffleEntry { PlayerName = fullName, Verified = true };
            state.Entries.Add(entry);
        }
        else if (!entry.PlayerName.Contains('@') && !string.IsNullOrEmpty(world))
        {
            entry.PlayerName = $"{PlayerInfoService.StripWorld(entry.PlayerName)}@{world}";
        }

        var available   = entry.CreditGil + gilReceived;
        var wanted      = (int)Math.Min(available / cost, RaffleGameIds.MaxTicketPoolSize);
        var granted     = wanted > 0 ? Allocate(state, entry.PlayerName, wanted) : 0;
        entry.CreditGil = available - (long)granted * cost;

        this.historyService.AddTransaction(new TransactionRecord
        {
            PlayerName = BuildTransactionName(entry, tradePartner),
            Amount     = (int)Math.Min(gilReceived, int.MaxValue),
            Timestamp  = DateTime.UtcNow,
            GameName   = RaffleGameIds.DisplayName,
        });
        Save();
        SessionUpdated?.Invoke();
        if (granted > 0)
            TicketsAutoGranted?.Invoke(entry.PlayerName);
    }

    private RaffleEntry? ResolvePayingEntry(RaffleState state, string tradePartner)
    {
        var direct = state.Entries.FirstOrDefault(e => NamesMatch(e.PlayerName, tradePartner));
        if (direct != null) return direct;
        return state.Entries.FirstOrDefault(e =>
            !string.IsNullOrEmpty(e.Buyer) &&
            PlayerInfoService.StripWorld(e.Buyer).Equals(tradePartner, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildTransactionName(RaffleEntry entry, string tradePartner)
    {
        var player = PlayerInfoService.StripWorld(entry.PlayerName);
        var buyer  = PlayerInfoService.StripWorld(entry.Buyer);
        var paidByBuyer = !string.IsNullOrEmpty(entry.Buyer)
                          && buyer.Equals(tradePartner, StringComparison.OrdinalIgnoreCase);
        return paidByBuyer ? $"{player} (Paid by {tradePartner})" : player;
    }

    private static int Allocate(RaffleState state, string owner, int requested)
    {
        if (requested <= 0) return 0;
        var ownerName       = PlayerInfoService.StripWorld(owner);
        var ownerTickets    = state.Blocks.Where(b => NamesMatch(b.Owner, ownerName)).Sum(b => b.Count);
        var playerRemaining = state.MaxTicketsPerPlayerAtStart - ownerTickets;
        if (playerRemaining <= 0) return 0;

        var granted = FreeNumbers(state).Take(Math.Min(requested, playerRemaining)).ToList();
        if (granted.Count == 0) return 0;

        // Merge the granted numbers with the owner's existing numbers, then rebuild their blocks so
        // that any newly contiguous runs (including reused gaps) coalesce into single ranges.
        var owned = new List<int>();
        foreach (var b in state.Blocks.Where(b => NamesMatch(b.Owner, ownerName)))
            for (var n = b.StartNumber; n <= b.EndNumber; n++) owned.Add(n);
        owned.AddRange(granted);

        state.Blocks.RemoveAll(b => NamesMatch(b.Owner, ownerName));
        foreach (var (start, count) in Coalesce(owned))
            state.Blocks.Add(new TicketBlock { Owner = owner, StartNumber = start, Count = count });

        return granted.Count;
    }

    // Yields the free ticket numbers in issue order: reusable gaps below the highest sold number
    // first (lowest first), then brand-new numbers extending the pool.
    private static IEnumerable<int> FreeNumbers(RaffleState state)
    {
        var occupied = new HashSet<int>();
        foreach (var b in state.Blocks)
            for (var n = b.StartNumber; n <= b.EndNumber; n++) occupied.Add(n);
        var highestSold = state.HighestNumber;
        for (var n = 1; n <= highestSold; n++)
            if (!occupied.Contains(n)) yield return n;
        for (var n = highestSold + 1; n <= RaffleGameIds.MaxTicketPoolSize; n++)
            yield return n;
    }

    private static IEnumerable<(int Start, int Count)> Coalesce(IEnumerable<int> numbers)
    {
        var sorted = numbers.Distinct().OrderBy(n => n).ToList();
        var i = 0;
        while (i < sorted.Count)
        {
            var start = sorted[i];
            var end   = start;
            i++;
            while (i < sorted.Count && sorted[i] == end + 1) { end = sorted[i]; i++; }
            yield return (start, end - start + 1);
        }
    }

    public bool AreNumbersHidden()
    {
        var state = this.config.RaffleSession;
        return state != null && state.ShuffleModeAtStart && !state.HasShuffled;
    }

    public void ShuffleTicketNumbers()
    {
        var state = this.config.RaffleSession;
        if (state == null || state.Blocks.Count == 0) return;

        var owners = new List<(string Owner, int Count)>();
        foreach (var b in state.Blocks.OrderBy(b => b.StartNumber))
        {
            var i = owners.FindIndex(o => NamesMatch(o.Owner, b.Owner));
            if (i >= 0) owners[i] = (owners[i].Owner, owners[i].Count + b.Count);
            else        owners.Add((b.Owner, b.Count));
        }

        var pool = Enumerable.Range(1, owners.Sum(o => o.Count)).ToList();
        for (var i = pool.Count - 1; i > 0; i--)
        {
            var j = Rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        state.Blocks.Clear();
        var cursor = 0;
        foreach (var (owner, count) in owners)
        {
            var numbers = pool.GetRange(cursor, count);
            cursor += count;
            foreach (var (start, runLength) in Coalesce(numbers))
                state.Blocks.Add(new TicketBlock { Owner = owner, StartNumber = start, Count = runLength });
        }

        state.HasShuffled = true;
        Save();
        SessionUpdated?.Invoke();
    }

    public int ComputeFreeTicketCount()
    {
        var state = this.config.RaffleSession;
        if (state == null) return RaffleGameIds.MaxTicketPoolSize;
        return RaffleGameIds.MaxTicketPoolSize - state.Blocks.Sum(b => b.Count);
    }

    public int GetPlayerTickets(string playerEntry)
    {
        var state = this.config.RaffleSession;
        if (state == null) return 0;
        var name = PlayerInfoService.StripWorld(playerEntry);
        return state.Blocks.Where(b => NamesMatch(b.Owner, name)).Sum(b => b.Count);
    }

    public List<TicketBlock> GetPlayerBlocks(string playerEntry)
    {
        var state = this.config.RaffleSession;
        if (state == null) return new();
        var name = PlayerInfoService.StripWorld(playerEntry);
        return state.Blocks.Where(b => NamesMatch(b.Owner, name)).OrderBy(b => b.StartNumber).ToList();
    }

    public string GetPlayerRangeLabel(string playerEntry)
    {
        if (AreNumbersHidden()) return "pending shuffle";
        var blocks = GetPlayerBlocks(playerEntry);
        return blocks.Count == 0 ? "-" : string.Join(", ", blocks.Select(b => b.RangeLabel));
    }

    public long GetPlayerCredit(string playerEntry) => FindEntry(playerEntry)?.CreditGil ?? 0L;

    public int ComputeTicketsSold()
        => this.config.RaffleSession?.Blocks.Sum(b => b.Count) ?? 0;

    public int ComputePlayersWithTickets() => ComputePlayersWithTickets(this.config);

    public static int ComputePlayersWithTickets(PluginConfiguration config)
    {
        var state = config.RaffleSession;
        if (state == null) return 0;
        return state.Entries.Count(e =>
        {
            var name = PlayerInfoService.StripWorld(e.PlayerName);
            return state.Blocks.Where(b => NamesMatch(b.Owner, name)).Sum(b => b.Count) > 0;
        });
    }

    public long ComputeTradesTotal()
    {
        var state = this.config.RaffleSession;
        return state == null ? 0L : state.TicketCostAtStart * ComputeTicketsSold();
    }

    public long ComputeTradesToPot()
    {
        var state = this.config.RaffleSession;
        return state == null ? 0L : ComputeTradesTotal() * state.TradesToPotPercentAtStart / 100;
    }

    public long ComputeTradesHeldBack() => ComputeTradesTotal() - ComputeTradesToPot();

    public long ComputeTotalPot() => ComputeTotalPot(this.config);

    public static long ComputeTotalPot(PluginConfiguration config)
    {
        var state = config.RaffleSession;
        if (state == null) return 0L;
        var ticketsSold = state.Blocks.Sum(b => b.Count);
        var tradesToPot = state.TicketCostAtStart * ticketsSold * state.TradesToPotPercentAtStart / 100;
        return tradesToPot + state.BoostedPotAtStart + state.PotAdjustment;
    }

    public bool CanDraw() => ComputeTicketsSold() > 0;

    public void ArmDraw()
    {
        var state = this.config.RaffleSession;
        if (state == null || !CanDraw()) return;
        if (state.ShuffleModeAtStart && !state.HasShuffled) ShuffleTicketNumbers();
        state.IsDrawArmed  = true;
        state.HasDrawn     = false;
        state.WinningNumber = null;
        state.WinnerName    = null;
        Save();
        SessionUpdated?.Invoke();
    }

    public void ClearDraw()
    {
        var state = this.config.RaffleSession;
        if (state == null) return;
        state.IsDrawArmed  = false;
        state.HasDrawn     = false;
        state.WinningNumber = null;
        state.WinnerName    = null;
        Save();
        SessionUpdated?.Invoke();
    }

    public bool TryCaptureDrawRoll(int rollValue)
    {
        var state = this.config.RaffleSession;
        if (state == null || !state.IsDrawArmed) return false;
        if (rollValue <= 0) return false;
        ApplyDraw(state, rollValue);
        return true;
    }

    public void SetWinnerManually(int number)
    {
        var state = this.config.RaffleSession;
        if (state == null || number <= 0) return;
        ApplyDraw(state, number);
    }

    private void ApplyDraw(RaffleState state, int number)
    {
        state.WinningNumber = number;
        state.WinnerName    = ResolveOwner(state, number);
        state.HasDrawn      = true;
        state.IsDrawArmed   = false;
        Save();
        SessionUpdated?.Invoke();
        if (state.WinnerName != null)
            WinnerDrawn?.Invoke(state.WinnerName, number, ComputeTotalPot());
    }

    private static string? ResolveOwner(RaffleState state, int number)
    {
        var block = state.Blocks.FirstOrDefault(b => b.Contains(number));
        return block?.Owner;
    }

    public long GetWinnerRemaining()
    {
        var state = this.config.RaffleSession;
        if (state?.WinnerName == null) return 0L;
        return Math.Max(0L, ComputeTotalPot() - state.WinnerPayoutGil);
    }

    public void TryRecordWinnerPayout(string partnerName, long amountSent)
    {
        var state = this.config.RaffleSession;
        if (state?.WinnerName == null) return;
        if (!PlayerInfoService.StripWorld(state.WinnerName).Equals(partnerName, StringComparison.OrdinalIgnoreCase)) return;
        state.WinnerPayoutGil += amountSent;
        state.PayoutTransactionId = PayoutTransactionRecorder.Record(this.historyService, RaffleGameIds.DisplayName, state.WinnerName!, state.WinnerPayoutGil, state.PayoutTransactionId);
        Save();
        SessionUpdated?.Invoke();
    }

    private RaffleEntry? FindEntry(string playerEntry)
    {
        var state = this.config.RaffleSession;
        if (state == null) return null;
        var name = PlayerInfoService.StripWorld(playerEntry);
        return state.Entries.FirstOrDefault(e => PlayerInfoService.StripWorld(e.PlayerName).Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool NamesMatch(string a, string b)
    {
        var parsedA = PlayerInfoService.StripWorld(a);
        var parsedB = PlayerInfoService.StripWorld(b);
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
