using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Utility;
using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Games.Bar777.Actions;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.UI.Components;
using System;
using System.IO;
using System.Numerics;
using MiniGamesEmporium.Games.Bar777.Services;

/// <summary>Draws the active game view for BAR 777.</summary>

namespace MiniGamesEmporium.Games.Bar777.UI.Tabs;
public sealed class Bar777GameTab
{
    private static readonly Vector4 GoldColour = new(1f, 0.84f, 0f, 1f);
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.Bar777Red;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);
    private const float TrophySide = 140f;
    private readonly ThemedCard card = new();
    private readonly PluginConfiguration config;
    private readonly Bar777SessionService bar777SessionService;
    private readonly ChatQueueService chatQueue;
    private readonly AutoPayoutService autoPayoutService;
    private readonly ISharedImmediateTexture? _trophyTexture;
    private int _pendingRollCount;
    private int _lastKnownAmountTraded = -1;
    private DateTime _lastKnownSessionStart;

    public Bar777GameTab(PluginConfiguration config, Bar777SessionService bar777SessionService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService)
    {
        this.config            = config;
        this.bar777SessionService    = bar777SessionService;
        this.chatQueue         = chatQueue;
        this.autoPayoutService = autoPayoutService;
        var path = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "trophy.png");
        if (File.Exists(path))
            _trophyTexture = MiniGamesEmporium.TextureProvider.GetFromFile(path);
    }

    public void Draw(bool skipLeadingSpacing = false)
    {
        if (!skipLeadingSpacing)
            ImGui.Spacing();
        var session = this.bar777SessionService.GetActiveSession();
        if (session == null || !Bar777GameIds.Matches(session.GameName))
            return;
        DrawActiveSessionView(session);
    }

    public void DrawSessionActionButtons()
    {
        using (UIHelper.PushBlueButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Scroll, "Send Rules", "##Bar777SendRules"))
                AnnounceRules.Execute(this.config, this.chatQueue);
        ImGui.SameLine();
        using (UIHelper.PushOrangeButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Advertise", "##Bar777Advertise"))
                Advertise.Execute(this.config, this.chatQueue);
    }

    private void DrawActiveSessionView(ActiveSession session)
    {
        if (Bar777GameIds.IsWaitingPlaceholder(session.PlayerName))
        {
            this.card.Draw("##Bar777WaitingCard", "Waiting for Players", CardAccent, CardTitle, DrawWaitingBody);
            return;
        }
        if (session.WinTriggered)
        {
            this.card.Draw("##Bar777WinnerCard", "Win Detected", CardAccent, GoldColour,
                () => DrawWinnerBody(session));
            return;
        }
        this.card.Draw("##Bar777PlayerCard", "Player", CardAccent, CardTitle, () => DrawPlayerBody(session));
        DrawTakeBetPhase(session);
        DrawRollsPhase(session);
        DrawSessionControls(session);
    }

    private static void DrawWaitingBody()
    {
        UIHelper.CentreTextDisabled($"Player: {Bar777GameIds.WaitingPlayerPlaceholder}");
    }

    private void DrawWinnerBody(ActiveSession session)
    {
        UIHelper.CentreTextScaled(BuildLockedDisplayName(session), GoldColour, 1.6f);

        ImGui.Spacing();
        DrawTrophy();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var pot       = Bar777SessionService.ComputeTotalPot(this.config);
        var paid      = session.WinnerPayoutGil;
        var remaining = Math.Max(0L, pot - paid);

        DrawPayoutFigures(pot, paid, remaining);
        ImGui.Spacing();

        var payoutRunning = this.autoPayoutService.IsRunning;
        var payoutIcon    = payoutRunning ? FontAwesomeIcon.Stop : FontAwesomeIcon.MoneyBillWave;
        var payoutLabel   = payoutRunning ? "Stop Auto Payout" : "Auto Payout";
        UIHelper.CentreNextButtonRow((FontAwesomeIcon.Coins, "Trade Winner"), (payoutIcon, payoutLabel));

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade Winner", "##WinnerTrade"))
                SendTradeRequest.Execute(session.PlayerName, this.chatQueue);
        }

        ImGui.SameLine();
        DrawAutoPayoutButton(session.PlayerName, remaining);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var progress = pot > 0 ? MathF.Min(1f, (float)paid / pot) : 1f;
        ImGui.ProgressBar(progress, new Vector2(-1f, ImGui.GetFrameHeight()), $"{progress * 100f:F0}% paid out");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.CentredIconTextButton(FontAwesomeIcon.FlagCheckered, "End Game", "##WinnerEndGame"))
            EndCurrentGame();
    }

    private void DrawTrophy()
    {
        var tex = this._trophyTexture?.GetWrapOrDefault();
        if (tex == null) return;
        var side = TrophySide * ImGuiHelpers.GlobalScale;
        UIHelper.CentreNext(side);
        ImGui.Image(tex.Handle, new Vector2(side, side));
    }

    private static void DrawPayoutFigures(long pot, long paid, long remaining)
    {
        var labelColW = MathF.Max(ImGui.CalcTextSize("Pot:").X, MathF.Max(ImGui.CalcTextSize("Traded:").X, ImGui.CalcTextSize("Remaining:").X));
        var valueColW = MathF.Max(ImGui.CalcTextSize($"{pot:N0} Gil").X, MathF.Max(ImGui.CalcTextSize($"{paid:N0} Gil").X, ImGui.CalcTextSize($"{remaining:N0} Gil").X));
        var spacing   = ImGui.GetStyle().ItemSpacing.X;
        var blockW    = labelColW + spacing + valueColW;
        var rowX      = ImGui.GetCursorPosX() + MathF.Max(0f, (ImGui.GetContentRegionAvail().X - blockW) * 0.5f);
        var valueX    = rowX + labelColW + spacing;

        DrawFigureRow(rowX, valueX, "Pot:",       $"{pot:N0} Gil",       GoldColour);
        DrawFigureRow(rowX, valueX, "Traded:",    $"{paid:N0} Gil",      EmporiumNeonTheme.SuccessMint);
        DrawFigureRow(rowX, valueX, "Remaining:", $"{remaining:N0} Gil", EmporiumNeonTheme.WarnAmber);
    }

    private static void DrawFigureRow(float rowX, float valueX, string label, string value, Vector4 colour)
    {
        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(colour, label);
        ImGui.SameLine(valueX);
        ImGui.TextColored(colour, value);
    }

    private void EndCurrentGame()
    {
        if (this.config.Bar777.UseQueue)
            this.bar777SessionService.EndQueuePlayerAndProcessNext();
        else
            this.bar777SessionService.EndWalkInAndReset();
    }

    private void DrawAutoPayoutButton(string playerName, long remaining)
    {
        if (this.autoPayoutService.IsRunning)
        {
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Auto Payout", "##Bar777StopAutoPayout"))
                this.autoPayoutService.Stop();
            return;
        }

        using var disabled = ImRaii.Disabled(remaining <= 0);
        using var green    = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.MoneyBillWave, "Auto Payout", "##Bar777AutoPayout"))
        {
            this.autoPayoutService.Start(
                playerName,
                () =>
                {
                    var p = Bar777SessionService.ComputeTotalPot(this.config);
                    var w = this.bar777SessionService.GetActiveSession()?.WinnerPayoutGil ?? 0L;
                    return Math.Max(0L, p - w);
                },
                () =>
                {
                    var s = this.bar777SessionService.GetActiveSession();
                    return s != null && Bar777GameIds.Matches(s.GameName);
                });
        }
    }

    private void DrawPlayerBody(ActiveSession session)
    {
        if (!this.config.Bar777.UseQueue && session.PlayerSet)
        {
            UIHelper.CentreTextScaled(BuildLockedDisplayName(session), EmporiumNeonTheme.SuccessMint, 1.4f);
            ImGui.Spacing();
            using (UIHelper.PushRedButtonColours())
            {
                if (UIHelper.CentredIconTextButton(FontAwesomeIcon.UserSlash, "Un-set Player", "##UnsetPlayer"))
                    this.bar777SessionService.UnlockWalkInPlayer();
            }
        }
        else
        {
            DrawUnlockedPlayerRow(session);
        }
    }

    private static void DrawStatusLine(ActiveSession session) =>
        UIHelper.CentreTextScaled(GetStatusText(session), GetStatusColour(session), 1.15f);

    private void DrawUnlockedPlayerRow(ActiveSession session)
    {
        var (charName, worldName) = GetCurrentTarget();
        string displayName;
        if (!this.config.Bar777.UseQueue && !string.IsNullOrEmpty(charName))
            displayName = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
        else
            displayName = Bar777GameIds.IsAnyPlaceholder(session.PlayerName) ? "Select a player to start" : BuildLockedDisplayName(session);

        UIHelper.CentreTextScaled(displayName, EmporiumNeonTheme.Bar777Red, 1.4f);

        var showSetBtn = !this.config.Bar777.UseQueue && !string.IsNullOrEmpty(charName);
        if (!showSetBtn) return;

        ImGui.Spacing();

        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.CentredIconTextButton(FontAwesomeIcon.UserCheck, "Set Player", "##SetWalkInPlayer"))
            this.bar777SessionService.LockWalkInPlayer(charName, worldName);
    }

    private static string GetStatusText(ActiveSession session)
    {
        if (session.WinTriggered) return "WIN DETECTED!";
        if (session.RollsUsed >= session.RollsAllowed) return "Session Complete";
        if (!session.PaymentVerified)
            return session.AmountTraded > 0
                ? $"Received {session.AmountTraded:N0} Gil - set rolls and start"
                : "Awaiting payment - set rolls and start";
        return $"Rolling  {session.RollsUsed} / {session.RollsAllowed}";
    }

    private static Vector4 GetStatusColour(ActiveSession session)
    {
        if (session.WinTriggered) return EmporiumNeonTheme.WinGold;
        if (session.RollsUsed >= session.RollsAllowed) return EmporiumNeonTheme.SuccessMint;
        if (!session.PaymentVerified) return EmporiumNeonTheme.WarnAmber;
        return EmporiumNeonTheme.NeonCyan;
    }

    private void DrawTakeBetPhase(ActiveSession session)
    {
        if (session.PaymentVerified) return;
        var pairHeight = this.card.MatchedHeight("##Bar777TakeBetCard", "##Bar777BuyerCard");
        using (var split = ImRaii.Table("##TakeBetSplit", 2, ImGuiTableFlags.None, new Vector2(-1f, 0f)))
        {
            if (split.Success)
            {
                ImGui.TableSetupColumn("##TakeBetLeft",  ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("##TakeBetRight", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                this.card.Draw("##Bar777TakeBetCard", "Take Bet", CardAccent, CardTitle, pairHeight, () => DrawPrimaryBetActions(session));
                ImGui.TableSetColumnIndex(1);
                this.card.Draw("##Bar777BuyerCard", "Paying for Another Player", CardAccent, CardTitle, pairHeight, () => DrawBuyerSection(session));
            }
        }
        this.card.Draw("##Bar777StartCard", "Start Game", CardAccent, CardTitle, () => DrawStartGameSection(session));
    }

    private void DrawPrimaryBetActions(ActiveSession session)
    {
        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Request Gil"),
            (FontAwesomeIcon.Coins, "Trade"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil", "##TellAmtBtn"))
            {
                if (this.config.Bar777.UseQueue)
                {
                    var tellName = BuildLockedDisplayName(session);
                    SendTellAmountRequest.Execute(tellName, this.config, this.chatQueue, session.AmountTraded);
                }
                else
                {
                    var (charName, worldName) = GetCurrentTarget();
                    if (!string.IsNullOrEmpty(charName))
                    {
                        this.bar777SessionService.LockWalkInPlayer(charName, worldName);
                        var tellName = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
                        SendTellAmountRequest.Execute(tellName, this.config, this.chatQueue, session.AmountTraded);
                    }
                }
            }
        }

        ImGui.SameLine();

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", "##TradeBtn"))
            {
                if (this.config.Bar777.UseQueue)
                {
                    if (!Bar777GameIds.IsAnyPlaceholder(session.PlayerName))
                        SendTradeRequest.Execute(session.PlayerName, this.chatQueue);
                }
                else
                {
                    var (charName, _) = GetCurrentTarget();
                    var targetName    = !string.IsNullOrEmpty(charName) ? charName
                                      : session.PlayerSet ? session.PlayerName : string.Empty;
                    if (!string.IsNullOrEmpty(targetName))
                        SendTradeRequest.Execute(targetName, this.chatQueue);
                }
            }
        }
    }

    private void SyncPendingRolls(ActiveSession session)
    {
        if (session.AmountTraded == this._lastKnownAmountTraded && session.StartedAt == this._lastKnownSessionStart) return;
        var costPerRoll = this.config.Bar777.CostPerRoll;
        this._pendingRollCount     = costPerRoll > 0
            ? Math.Min(session.AmountTraded / costPerRoll, this.config.Bar777.MaxRolls)
            : 0;
        this._lastKnownAmountTraded = session.AmountTraded;
        this._lastKnownSessionStart = session.StartedAt;
    }

    private void DrawBuyerSection(ActiveSession session)
    {
        var buyer = this.bar777SessionService.GetBuyer();
        if (!string.IsNullOrEmpty(buyer))
        {
            DrawAssignedBuyer(session, buyer);
            return;
        }

        var (charName, worldName) = GetCurrentTarget();
        if (string.IsNullOrEmpty(charName))
        {
            UIHelper.CentreTextDisabled("Target a player in-game to set them as the buyer.");
            return;
        }

        var style        = ImGui.GetStyle();
        var targetedRowW = ImGui.CalcTextSize("Targeted:").X + style.ItemSpacing.X + ImGui.CalcTextSize(charName).X;
        UIHelper.CentreNext(targetedRowW);
        ImGui.TextDisabled("Targeted:");
        ImGui.SameLine();
        ImGui.TextUnformatted(charName);
        ImGui.Spacing();

        using (UIHelper.PushGreenButtonColours())
        {
            if (UIHelper.CentredIconTextButton(FontAwesomeIcon.UserCheck, "Set as Buyer", "##SetBuyer"))
            {
                var fullName = string.IsNullOrEmpty(worldName) ? charName : $"{charName}@{worldName}";
                this.bar777SessionService.SetBuyer(fullName);
            }
        }
    }

    private void DrawAssignedBuyer(ActiveSession session, string buyer)
    {
        var style     = ImGui.GetStyle();
        var clearBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Times, "Clear").X;
        var rowW      = ImGui.CalcTextSize("Buyer:").X + style.ItemSpacing.X
                      + ImGui.CalcTextSize(buyer).X    + style.ItemSpacing.X
                      + clearBtnW;
        UIHelper.CentreNext(rowW);
        ImGui.TextDisabled("Buyer:");
        ImGui.SameLine();
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, buyer);
        ImGui.SameLine();
        using (UIHelper.PushRedButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Clear", "##ClearBuyer"))
                this.bar777SessionService.ClearBuyer();
        }
        ImGui.Spacing();

        UIHelper.CentreNextButtonRow(
            (FontAwesomeIcon.CommentDots, "Request Gil (Buyer)"),
            (FontAwesomeIcon.Coins, "Trade (Buyer)"));

        using (UIHelper.PushBlueButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", "##BuyerRequestGil"))
                SendTellBuyerRequest.Execute(buyer, session.PlayerName, this.config, this.chatQueue);
        }
        ImGui.SameLine();

        using (UIHelper.PushAmberButtonColours())
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade (Buyer)", "##BuyerTrade"))
                SendTradeRequest.Execute(buyer, this.chatQueue);
        }
    }

    private void DrawStartGameSection(ActiveSession session)
    {
        SyncPendingRolls(session);

        string descText;
        if (session.AmountTraded > 0)
        {
            var costPerRoll = this.config.Bar777.CostPerRoll;
            var calculated  = costPerRoll > 0 ? Math.Min(session.AmountTraded / costPerRoll, this.config.Bar777.MaxRolls) : 0;
            descText = $"{session.AmountTraded:N0} Gil received - {calculated} roll(s) at {costPerRoll:N0}/roll";
        }
        else
        {
            descText = "No trade recorded. Enter roll count manually.";
        }
        DrawStatusLine(session);
        ImGui.Spacing();
        UIHelper.CentreTextDisabled(descText);
        ImGui.Spacing();

        const float InputW = 160f;
        UIHelper.CentreNext(InputW + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.CalcTextSize("Rolls").X);
        ImGui.SetNextItemWidth(InputW);
        ImGui.InputInt("Rolls##PendingRolls", ref this._pendingRollCount, 1, 1);
        this._pendingRollCount = Math.Clamp(this._pendingRollCount, 0, this.config.Bar777.MaxRolls);

        var noPlayer = !this.config.Bar777.UseQueue && !session.PlayerSet;
        if (noPlayer)
            UIHelper.CentreTextDisabled("Target a player first to lock them in.");
        ImGui.Spacing();

        using var disabled = ImRaii.Disabled(this._pendingRollCount < 1 || noPlayer);
        using var green    = UIHelper.PushGreenButtonColours();
        if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Play, "Start Game", "##StartGame"))
            this.bar777SessionService.StartGameWithRolls(this._pendingRollCount);
    }

    private void DrawRollsPhase(ActiveSession session)
    {
        if (!session.PaymentVerified) return;
        this.card.Draw("##Bar777RollsCard", "Rolls", CardAccent, CardTitle, () => DrawRollsBody(session));
    }

    private void DrawRollsBody(ActiveSession session)
    {
        DrawStatusLine(session);
        ImGui.Spacing();

        if (session.AmountTraded > 0)
        {
            UIHelper.CentreText($"Session Trade: {session.AmountTraded:N0} Gil", EmporiumNeonTheme.NeonCyan);
            ImGui.Spacing();
        }
        if (!this.config.Bar777.Chat.AutoStartRolls)
        {
            using (UIHelper.PushYellowButtonColours())
            {
                if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Comments, "Send 'Start Rolls' Msg", "##StartRollsMsg"))
                    AnnouncePaymentReceived.Execute(session.PlayerName, this.config, this.chatQueue);
            }
            ImGui.Spacing();
        }
        var progress = session.RollsAllowed > 0
            ? (float)session.RollsUsed / session.RollsAllowed
            : 0f;
        ImGui.ProgressBar(progress, new Vector2(-1f, 0f), string.Empty);
        ImGui.Spacing();
        DrawRollLog(session);
    }

    private void DrawRollLog(ActiveSession session)
    {
        var log = session.RollLog;
        const int maxRows = 5;
        var lineH   = ImGui.GetTextLineHeight() + ImGui.GetStyle().ItemSpacing.Y;
        var padding = ImGui.GetStyle().WindowPadding.Y * 2f;
        var height  = maxRows * lineH + padding;
        using var child = ImRaii.Child("##RollLogBox", new Vector2(-1, height), true);
        if (!child.Success) return;
        if (log == null || log.Count == 0)
        {
            ImGui.TextDisabled("No rolls yet.");
            return;
        }
        var numCols = Math.Max(1, (log.Count + maxRows - 1) / maxRows);
        using var table = ImRaii.Table("##RollGrid", numCols);
        if (!table.Success) return;
        int? deleteIndex = null;
        for (var row = 0; row < maxRows; row++)
        {
            ImGui.TableNextRow();
            for (var col = 0; col < numCols; col++)
            {
                var idx = col * maxRows + row;
                if (idx >= log.Count) continue;
                ImGui.TableSetColumnIndex(col);
                if (DrawRollEntry(log, idx))
                    deleteIndex = idx;
            }
        }
        if (deleteIndex.HasValue)
            this.bar777SessionService.RemoveRoll(deleteIndex.Value);
    }

    private bool DrawRollEntry(System.Collections.Generic.List<int> log, int index)
    {
        var roll  = log[index];
        var isWin = roll == this.config.Bar777.WinNumber;
        var label = $"Roll {index + 1,2}: {roll,4}";
        if (isWin)
            ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{label}  WIN!");
        else
            ImGui.TextDisabled(label);
        var deleteRequested = false;
        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            ImGui.TextUnformatted("Ctrl+Click to remove this roll");
        }
        if (ImGui.IsItemClicked() && ImGui.GetIO().KeyCtrl)
            deleteRequested = true;
        return deleteRequested;
    }

    private void DrawSessionControls(ActiveSession session)
    {
        var sessionDone = session.RollsUsed >= session.RollsAllowed || session.WinTriggered;
        if (!(session.PaymentVerified && !sessionDone) && !sessionDone) return;
        this.card.Draw("##Bar777SessionCard", "Session", CardAccent, CardTitle,
            () => DrawSessionControlsBody(session, sessionDone));
    }

    private void DrawSessionControlsBody(ActiveSession session, bool sessionDone)
    {
        if (session.PaymentVerified && !sessionDone)
        {
            using (UIHelper.PushRedButtonColours())
            {
                if (UIHelper.CentredIconTextButton(FontAwesomeIcon.ExclamationTriangle, "End Game Early", "##EndEarlyBtn"))
                    ImGui.OpenPopup("Confirm End Game Early##Modal");
            }
            using var modal = ImRaii.PopupModal("Confirm End Game Early##Modal", ImGuiWindowFlags.AlwaysAutoResize);
            if (modal.Success)
            {
                ImGui.TextUnformatted("The player hasn't finished their rolls.");
                ImGui.TextUnformatted("Are you sure you want to end their game?");
                ImGui.Spacing();
                using (UIHelper.PushGreenButtonColours())
                {
                    if (UIHelper.IconTextButton(FontAwesomeIcon.Check, "Yes, end early", "##ConfirmEndEarly"))
                    {
                        EndCurrentGame();
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.SameLine();
                using (UIHelper.PushRedButtonColours())
                {
                    if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##CancelEndEarly"))
                        ImGui.CloseCurrentPopup();
                }
            }
            ImGui.Spacing();
        }

        if (!sessionDone) return;

        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.CentredIconTextButton(FontAwesomeIcon.FlagCheckered, "End Game", "##EndGameBtn"))
            EndCurrentGame();
    }

    private static (string CharName, string WorldName) GetCurrentTarget()
    {
        var playerChar = MiniGamesEmporium.TargetManager.Target as IPlayerCharacter;
        if (playerChar == null) return (string.Empty, string.Empty);
        var charName  = playerChar.Name.TextValue;
        var worldName = playerChar.HomeWorld.Value.Name.ToString();
        return (charName, worldName);
    }

    private static string BuildLockedDisplayName(ActiveSession session)
    {
        var world = session.PlayerWorld;
        return string.IsNullOrEmpty(world) ? session.PlayerName : $"{session.PlayerName}@{world}";
    }
}
