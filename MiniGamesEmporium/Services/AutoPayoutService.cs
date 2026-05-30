using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MiniGamesEmporium.Actions;
using System;

/// <summary>Automates sequential winner payouts in 1,000,000 gil chunks, interacting with the Trade, InputNumeric, and SelectYesno addons via ECommons callbacks. Cancels automatically if the trade is declined.</summary>

namespace MiniGamesEmporium.Services;
public sealed unsafe class AutoPayoutService : IDisposable
{
    private const long MaxChunkGil = 1_000_000L;
    private const int ChunkTimeoutSeconds = 120;

    private readonly ChatQueueService chatQueue;
    private readonly IPluginLog log;
    private readonly TaskManager taskManager = new();

    private string _targetName = string.Empty;
    private Func<long>? _getRemainingFn;
    private Func<bool>? _isSessionActiveFn;
    private volatile bool _tradeCompleted;
    private volatile bool _tradeCancelled;
    private DateTime _chunkDeadline;
    private int _gilTick;

    public bool IsRunning { get; private set; }

    public AutoPayoutService(ChatQueueService chatQueue, IPluginLog log)
    {
        this.chatQueue = chatQueue;
        this.log = log;
        TradeDetectionManager.OnTradeEnd += OnTradeEnd;
    }

    public void Start(string targetName, Func<long> getRemainingFn, Func<bool> isSessionActiveFn)
    {
        if (IsRunning) return;
        _targetName = targetName;
        _getRemainingFn = getRemainingFn;
        _isSessionActiveFn = isSessionActiveFn;
        IsRunning = true;
        EnqueueChunk();
    }

    public void Stop()
    {
        if (IsRunning)
            log.Information("[AutoPayout] Stopped.");
        IsRunning = false;
        _tradeCompleted = false;
        _tradeCancelled = false;
        taskManager.Abort();
    }

    private void Finish(string logMessage)
    {
        log.Information("[AutoPayout] {Message}", logMessage);
        IsRunning = false;
        _tradeCompleted = false;
        _tradeCancelled = false;
    }

    private void EnqueueChunk()
    {
        if (_isSessionActiveFn?.Invoke() == false) { Finish("Session is no longer active - stopping payout."); return; }
        var remaining = _getRemainingFn?.Invoke() ?? 0L;
        if (remaining <= 0L) { Stop(); return; }

        var chunk = (int)Math.Min(remaining, MaxChunkGil);
        _tradeCompleted = false;
        _tradeCancelled = false;
        _gilTick = 0;
        _chunkDeadline = DateTime.UtcNow.AddSeconds(ChunkTimeoutSeconds);

        log.Information("[AutoPayout] Sending trade request to '{Target}' for {Chunk} gil ({Remaining} remaining).", _targetName, chunk, remaining);
        SendTradeRequest.Execute(_targetName, this.chatQueue);
        taskManager.Enqueue(() => SetGilAmount(chunk));
        taskManager.EnqueueDelay(300);
        taskManager.Enqueue(() => ClickTradeAccept());
        taskManager.Enqueue(() => AwaitAndConfirmSelectYesno());
        taskManager.Enqueue(() => AwaitTradeEnd());
        taskManager.Enqueue(() => OnChunkComplete());
    }

    private bool? SetGilAmount(int chunk)
    {
        if (_tradeCancelled || IsExpired()) return true;

        var inAddon = Svc.GameGui.GetAddonByName("InputNumeric", 1);
        if (!inAddon.IsNull && ((AtkUnitBase*)inAddon.Address)->IsVisible)
        {
            new AddonMaster.InputNumeric(inAddon.Address).Ok(chunk);
            return true;
        }

        var tradeAddon = Svc.GameGui.GetAddonByName("Trade", 1);
        if (tradeAddon.IsNull || !((AtkUnitBase*)tradeAddon.Address)->IsVisible) return false;
        
        if (++_gilTick % 60 == 1)
            Callback.Fire((AtkUnitBase*)tradeAddon.Address, true, 2);

        return false;
    }

    private bool? ClickTradeAccept()
    {
        if (_tradeCancelled || IsExpired()) return true;
        var tradeAddon = Svc.GameGui.GetAddonByName("Trade", 1);
        if (tradeAddon.IsNull) return false;
        var trade = (AtkUnitBase*)tradeAddon.Address;
        if (!trade->IsVisible) return false;
        Callback.Fire(trade, true, 0);
        return true;
    }

    private bool? AwaitAndConfirmSelectYesno()
    {
        if (_tradeCancelled || IsExpired()) return true;
        var yesnoAddon = Svc.GameGui.GetAddonByName("SelectYesno", 1);
        if (yesnoAddon.IsNull) return false;
        var addon = (AtkUnitBase*)yesnoAddon.Address;
        if (!addon->IsVisible) return false;
        new AddonMaster.SelectYesno(yesnoAddon.Address).Yes();
        return true;
    }

    private bool? AwaitTradeEnd()
    {
        if (_tradeCancelled || _tradeCompleted || IsExpired()) return true;
        return false;
    }

    private bool? OnChunkComplete()
    {
        if (!IsRunning) return true;

        if (_tradeCancelled)
        {
            Finish("Trade was cancelled by the other party - stopping payout.");
            return true;
        }

        if (IsExpired())
        {
            Finish($"Chunk timed out after {ChunkTimeoutSeconds}s - stopping payout.");
            return true;
        }

        if (_isSessionActiveFn?.Invoke() == false)
        {
            Finish("Session is no longer active - stopping payout.");
            return true;
        }

        var remaining = _getRemainingFn?.Invoke() ?? 0L;
        if (remaining <= 0L)
        {
            Finish("Payout complete - all gil transferred.");
            return true;
        }

        taskManager.EnqueueDelay(1500);
        taskManager.Enqueue(() => { EnqueueChunk(); return true; });
        return true;
    }

    private bool IsExpired() => DateTime.UtcNow >= _chunkDeadline;

    private void OnTradeEnd(IPlayerCharacter? counterparty, TradeDetectionManager.TradeDescriptor? result)
    {
        if (!IsRunning) return;
        if (result == null)
            _tradeCancelled = true;
        else
            _tradeCompleted = true;
    }

    public void Dispose()
    {
        TradeDetectionManager.OnTradeEnd -= OnTradeEnd;
        taskManager.Dispose();
    }
}
