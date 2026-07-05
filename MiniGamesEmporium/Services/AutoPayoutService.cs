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

/// <summary>Pays out winners in 1,000,000 gil chunks.</summary>

namespace MiniGamesEmporium.Services;
public sealed unsafe class AutoPayoutService : IDisposable
{
    private const long MaxChunkGil = 1_000_000L;

    private readonly ChatQueueService chatQueue;
    private readonly IPluginLog log;
    private readonly TaskManager taskManager = new();

    private string _targetName = string.Empty;
    private Func<long>? _getRemainingFn;
    private Func<bool>? _isSessionActiveFn;
    private volatile bool _awaitingResult;
    private int _gilTick;

    public bool IsRunning { get; private set; }

    public bool IsRunningFor(string targetName) =>
        IsRunning && _targetName.Equals(targetName, StringComparison.OrdinalIgnoreCase);

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
            log.Information("[AutoPayout] Stopped - no further chunks will be sent.");
        IsRunning = false;
        _awaitingResult = false;
        taskManager.Abort();
    }

    private void Finish(string logMessage)
    {
        log.Information("[AutoPayout] {Message}", logMessage);
        IsRunning = false;
        _awaitingResult = false;
    }

    private void EnqueueChunk()
    {
        if (_isSessionActiveFn?.Invoke() == false) { Finish("Session is no longer active - stopping payout."); return; }
        var remaining = _getRemainingFn?.Invoke() ?? 0L;
        if (remaining <= 0L) { Finish("Payout complete - all gil transferred."); return; }

        var chunk = (int)Math.Min(remaining, MaxChunkGil);
        _gilTick = 0;
        _awaitingResult = true;

        log.Information("[AutoPayout] Sending trade request to '{Target}' for {Chunk} gil ({Remaining} remaining).", _targetName, chunk, remaining);
        SendTradeRequest.Execute(_targetName, this.chatQueue);
        taskManager.Enqueue(() => SetGilAmount(chunk));
        taskManager.EnqueueDelay(300);
        taskManager.Enqueue(() => ClickTradeAccept());
        taskManager.Enqueue(() => AwaitAndConfirmSelectYesno());
    }

    private bool? SetGilAmount(int chunk)
    {
        if (!_awaitingResult) return true;

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
        if (!_awaitingResult) return true;
        var tradeAddon = Svc.GameGui.GetAddonByName("Trade", 1);
        if (tradeAddon.IsNull) return false;
        var trade = (AtkUnitBase*)tradeAddon.Address;
        if (!trade->IsVisible) return false;
        Callback.Fire(trade, true, 0);
        return true;
    }

    private bool? AwaitAndConfirmSelectYesno()
    {
        if (!_awaitingResult) return true;
        var yesnoAddon = Svc.GameGui.GetAddonByName("SelectYesno", 1);
        if (yesnoAddon.IsNull) return false;
        var addon = (AtkUnitBase*)yesnoAddon.Address;
        if (!addon->IsVisible) return false;
        new AddonMaster.SelectYesno(yesnoAddon.Address).Yes();
        return true;
    }

    private void OnTradeEnd(IPlayerCharacter? counterparty, TradeDetectionManager.TradeDescriptor? result)
    {
        if (!_awaitingResult) return;
        if (counterparty != null && !counterparty.Name.TextValue.Equals(_targetName, StringComparison.OrdinalIgnoreCase)) return;

        _awaitingResult = false;
        if (!IsRunning) return;

        if (result == null)
        {
            Finish("Trade was cancelled or declined - stopping payout.");
            return;
        }

        var given = result.ReceivedGil < 0 ? -(long)result.ReceivedGil : 0L;
        if (given <= 0L)
        {
            Finish("Trade completed but no gil was sent - stopping payout.");
            return;
        }

        if (_isSessionActiveFn?.Invoke() == false)
        {
            Finish("Session is no longer active - stopping payout.");
            return;
        }

        var remaining = _getRemainingFn?.Invoke() ?? 0L;
        if (remaining <= 0L)
        {
            Finish("Payout complete - all gil transferred.");
            return;
        }

        taskManager.EnqueueDelay(1500);
        taskManager.Enqueue(() => { EnqueueChunk(); return true; });
    }

    public void Dispose()
    {
        TradeDetectionManager.OnTradeEnd -= OnTradeEnd;
        taskManager.Dispose();
    }
}
