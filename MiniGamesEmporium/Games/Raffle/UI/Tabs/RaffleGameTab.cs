using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Raffle.Actions;
using MiniGamesEmporium.Games.Raffle.Services;
using MiniGamesEmporium.Games.Raffle.State;
using MiniGamesEmporium.Games.Raffle.Utility;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using System;
using System.IO;
using System.Numerics;

/// <summary>Draws the Raffle registration screen, ticket pool, pot controls and the winner draw.</summary>

namespace MiniGamesEmporium.Games.Raffle.UI.Tabs;
public sealed class RaffleGameTab
{
    private const float RightPaneW = 340f;
    private const float ActionsColW = 500f;
    private const float TrophySide = 140f;

    private static readonly Vector4 GoldColour = new(1f, 0.84f, 0f, 1f);

    private static readonly Vector4 PurpleBtn        = new(0.44f, 0.12f, 0.66f, 1f);
    private static readonly Vector4 PurpleBtnHovered = new(0.56f, 0.18f, 0.80f, 1f);
    private static readonly Vector4 PurpleBtnActive  = new(0.32f, 0.09f, 0.48f, 1f);
    private static readonly Vector4 PinkBtn          = new(0.85f, 0.16f, 0.52f, 1f);
    private static readonly Vector4 PinkBtnHovered   = new(1.00f, 0.28f, 0.64f, 1f);
    private static readonly Vector4 PinkBtnActive    = new(0.62f, 0.10f, 0.38f, 1f);
    private static readonly Vector4 GoldBtn          = new(0.72f, 0.55f, 0.00f, 1f);
    private static readonly Vector4 GoldBtnHovered   = new(0.88f, 0.68f, 0.00f, 1f);
    private static readonly Vector4 GoldBtnActive    = new(0.58f, 0.44f, 0.00f, 1f);
    private static readonly Vector4 TealBtn          = new(0.06f, 0.55f, 0.55f, 1f);
    private static readonly Vector4 TealBtnHovered   = new(0.10f, 0.72f, 0.72f, 1f);
    private static readonly Vector4 TealBtnActive    = new(0.04f, 0.42f, 0.42f, 1f);
    private static readonly Vector4 MagentaBtn       = new(0.55f, 0.12f, 0.66f, 1f);
    private static readonly Vector4 MagentaBtnHover   = new(0.70f, 0.18f, 0.82f, 1f);
    private static readonly Vector4 MagentaBtnActive = new(0.40f, 0.09f, 0.50f, 1f);

    private readonly PluginConfiguration config;
    private readonly RaffleService service;
    private readonly ChatQueueService chatQueue;
    private readonly HistoryService historyService;
    private readonly AutoPayoutService autoPayoutService;
    private readonly ISharedImmediateTexture? trophyTexture;

    private string comboFilter = string.Empty;
    private int donationInput = 0;
    private int manualDrawInput = 0;
    private int ticketAddInput = 1;

    public RaffleGameTab(
        PluginConfiguration config,
        RaffleService service,
        ChatQueueService chatQueue,
        HistoryService historyService,
        AutoPayoutService autoPayoutService)
    {
        this.config            = config;
        this.service           = service;
        this.chatQueue         = chatQueue;
        this.historyService    = historyService;
        this.autoPayoutService = autoPayoutService;
        var path = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "trophy.png");
        if (File.Exists(path))
            this.trophyTexture = MiniGamesEmporium.TextureProvider.GetFromFile(path);
    }

    public void Draw()
    {
        var state = this.service.GetState();
        if (state == null) return;
        ImGui.Spacing();

        if (state.HasDrawn && state.WinnerName != null)
        {
            DrawRaffleComplete(state);
            return;
        }

        var splitH = MathF.Max(120f, ImGui.GetContentRegionAvail().Y);
        using var split = ImRaii.Table("##RaffleSplit", 2,
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV, new Vector2(-1f, splitH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##RafflePlayerCol", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##RaffleSideCol",   ImGuiTableColumnFlags.WidthFixed, RightPaneW);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        var leftH = ImGui.GetContentRegionAvail().Y;
        using (var left = ImRaii.Child("##RafflePlayerPane", new Vector2(-1f, leftH), false, ImGuiWindowFlags.NoScrollbar))
            if (left.Success) DrawLeftColumn(state);
        ImGui.TableSetColumnIndex(1);
        var rightH = ImGui.GetContentRegionAvail().Y;
        using (var right = ImRaii.Child("##RaffleSidePane", new Vector2(-1f, rightH), false, ImGuiWindowFlags.NoScrollbar))
            if (right.Success) DrawSidePane(state);
    }

    private void DrawLeftColumn(RaffleState state)
    {
        var showKept = state.TradesToPotPercentAtStart < 100;
        var bottomH  = GetStatsHeight(showKept);
        var listH    = MathF.Max(80f, ImGui.GetContentRegionAvail().Y - bottomH - ImGui.GetStyle().ItemSpacing.Y);
        using (var listChild = ImRaii.Child("##RaffleListRegion", new Vector2(-1f, listH), false, ImGuiWindowFlags.NoScrollbar))
            if (listChild.Success) DrawPlayerList(state);
        DrawBottomStats(state, showKept);
    }

    private static float GetStatsHeight(bool showKept)
    {
        var rowH   = ImGui.GetTextLineHeight() + ImGui.GetStyle().CellPadding.Y * 2f;
        var inputH = ImGui.GetFrameHeight()    + ImGui.GetStyle().CellPadding.Y * 2f;
        var rows   = 5 + (showKept ? 1 : 0);
        return rows * rowH + inputH + ImGui.GetStyle().WindowPadding.Y * 2f + 4f;
    }

    private void DrawPlayerList(RaffleState state)
    {
        var availH = ImGui.GetContentRegionAvail().Y;
        var startY = ImGui.GetCursorPosY();
        ImGui.TextColored(EmporiumNeonTheme.RaffleTeal, $"Players ({this.service.ComputePlayersWithTickets()} / {state.Entries.Count} with tickets)");
        ImGui.Spacing();
        DrawAddPlayerCombo();
        ImGui.Spacing();
        var tableH = MathF.Max(24f, availH - (ImGui.GetCursorPosY() - startY));
        if (state.Entries.Count == 0)
        {
            ImGui.TextDisabled("No players added yet.");
            return;
        }
        int removeIdx = -1, tradeIdx = -1;
        using var table = ImRaii.Table("##RafflePlayerTable", 3,
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(-1f, tableH));
        if (!table.Success) return;
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("#",            ImGuiTableColumnFlags.WidthFixed, 26f);
        ImGui.TableSetupColumn("Player",       ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##RaffleActs", ImGuiTableColumnFlags.WidthFixed, ActionsColW);
        ImGui.TableHeadersRow();
        for (var i = 0; i < state.Entries.Count; i++)
        {
            ImGui.TableNextRow();
            DrawPlayerRow(state, i, ref removeIdx, ref tradeIdx);
        }
        if (tradeIdx  >= 0) SendTradeRequest.Execute(PlayerInfoService.StripWorld(state.Entries[tradeIdx].PlayerName), this.chatQueue);
        if (removeIdx >= 0) this.service.RemovePlayer(removeIdx);
    }

    private void DrawPlayerRow(RaffleState state, int idx, ref int removeIdx, ref int tradeIdx)
    {
        var entry       = state.Entries[idx];
        var displayName = PlayerInfoService.StripWorld(entry.PlayerName);
        var tickets     = this.service.GetPlayerTickets(entry.PlayerName);
        var ranges      = this.service.GetPlayerRangeLabel(entry.PlayerName);
        var credit      = entry.CreditGil;
        var isFree      = state.TicketCostAtStart == 0;
        var hasTickets  = tickets > 0;
        var atMax       = tickets >= state.MaxTicketsPerPlayerAtStart;

        if (hasTickets)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.04f, 0.20f, 0.18f, 1f)));

        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled($"{idx + 1}");

        ImGui.TableSetColumnIndex(1);
        var nameColour = hasTickets ? new Vector4(0.22f, 0.82f, 0.62f, 1f)
                                    : new Vector4(0.94f, 0.92f, 0.98f, 1f);
        ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
        ImGui.TextColored(nameColour, displayName);
        ImGui.TextDisabled($"Tickets: {tickets}   Numbers: {ranges}");
        ImGui.PopTextWrapPos();
        if (credit > 0)
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"+{credit:N0} gil overpay");

        ImGui.TableSetColumnIndex(2);

        if (hasTickets)
        {
            using (UIHelper.PushBlueButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Send Numbers", $"##RaffleNumbers{idx}"))
                    TellTicketNumbers.Execute(entry.PlayerName, ranges, this.config, this.chatQueue);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Tell {displayName} their ticket numbers");
            ImGui.SameLine();
        }

        {
            var spacing   = ImGui.GetStyle().ItemSpacing.X;
            var totalBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.TicketAlt, "Tickets").X
                          + UIHelper.CalcButtonSize(FontAwesomeIcon.Trash, "").X;
            var numBtns = 2;
            if (!isFree && !atMax) { totalBtnW += UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request").X + UIHelper.CalcButtonSize(FontAwesomeIcon.Coins, "Trade").X; numBtns += 2; }
            if (!isFree && !atMax) { totalBtnW += UIHelper.CalcButtonSize(FontAwesomeIcon.Gift, "").X; numBtns++; }
            totalBtnW += (numBtns - 1) * spacing;
            var rightEdge = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), rightEdge - totalBtnW));
        }
        if (!isFree && !atMax)
        {
            using (UIHelper.PushBlueButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request", $"##RaffleReq{idx}"))
                    RequestGil.Execute(entry.PlayerName, this.config, this.chatQueue);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Send a /tell requesting the ticket cost");
            ImGui.SameLine();
            using (UIHelper.PushAmberButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", $"##RaffleTrade{idx}"))
                    tradeIdx = idx;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Send a trade request - a completed trade grants tickets automatically");
            ImGui.SameLine();
        }

        using (Btn(PurpleBtn, PurpleBtnHovered, PurpleBtnActive))
            if (UIHelper.IconTextButton(FontAwesomeIcon.TicketAlt, "Tickets", $"##RaffleTickets{idx}"))
                ImGui.OpenPopup($"##RaffleTicketPopup{idx}");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Manually grant or remove tickets (adds their value to the pot)");
        ImGui.SameLine();

        if (!isFree && !atMax)
        {
            DrawBuyerButton(entry.PlayerName, idx);
            ImGui.SameLine();
        }

        ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.60f, 0.08f, 0.08f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.80f, 0.12f, 0.12f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.45f, 0.06f, 0.06f, 1f));
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "", $"##RaffleRem{idx}") && ImGui.GetIO().KeyCtrl)
            removeIdx = idx;
        ImGui.PopStyleColor(3);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hold Ctrl and click to remove player (their numbers become gaps)");

        DrawTicketPopup(idx, entry.PlayerName);
        DrawBuyerPopup(idx, entry.PlayerName);
    }

    private void DrawTicketPopup(int idx, string playerEntry)
    {
        using var popup = ImRaii.Popup($"##RaffleTicketPopup{idx}");
        if (!popup.Success) return;
        var state    = this.service.GetState();
        var owned    = this.service.GetPlayerTickets(playerEntry);
        var maxPer   = state?.MaxTicketsPerPlayerAtStart ?? RaffleGameIds.MaxTicketPoolSize;
        var poolLeft = this.service.ComputeFreeTicketCount();
        var canBuy   = Math.Max(0, Math.Min(maxPer - owned, poolLeft));

        ImGui.TextColored(EmporiumNeonTheme.RaffleTeal, $"Tickets: {PlayerInfoService.StripWorld(playerEntry)}");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled($"Owns {owned} ticket(s): {this.service.GetPlayerRangeLabel(playerEntry)}");
        ImGui.Spacing();
        if (canBuy > 0)
        {
            if (ImGui.IsWindowAppearing())
                this.ticketAddInput = canBuy;
            this.ticketAddInput = Math.Clamp(this.ticketAddInput, 1, canBuy);
            ImGui.TextDisabled($"Add tickets (grants the next free numbers, max {canBuy})");
            ImGui.SetNextItemWidth(140f);
            if (ImGui.InputInt("##RaffleTicketAdd", ref this.ticketAddInput, 1, 5))
                this.ticketAddInput = Math.Clamp(this.ticketAddInput, 1, canBuy);
            ImGui.SameLine();
            using (UIHelper.PushGreenButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Add", $"##RaffleTicketAddBtn{idx}"))
                    this.service.AddTicketsManual(playerEntry, this.ticketAddInput);
        }
        else
        {
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "At maximum tickets - none can be added.");
        }
        ImGui.Spacing();
        using (UIHelper.PushRedButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Minus, "Remove last ticket block", $"##RaffleTicketRem{idx}"))
                this.service.RemoveLastTicketBlock(playerEntry);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Frees the most recent block of numbers (leaves a gap)");
    }

    private void DrawBuyerButton(string playerEntry, int idx)
    {
        var hasBuyer = !string.IsNullOrEmpty(this.service.GetPlayerBuyer(playerEntry));
        if (hasBuyer)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.SuccessMint with { W = 0.8f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, EmporiumNeonTheme.SuccessMint);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  EmporiumNeonTheme.SuccessMint with { W = 0.6f });
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        PinkBtn);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PinkBtnHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  PinkBtnActive);
        }
        if (UIHelper.IconTextButton(FontAwesomeIcon.Gift, "", $"##RaffleBuyerBtn{idx}"))
            ImGui.OpenPopup($"##RaffleBuyerPopup{idx}");
        ImGui.PopStyleColor(3);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(hasBuyer ? $"Buyer: {this.service.GetPlayerBuyer(playerEntry)}" : "Set a buyer to pay for this player");
    }

    private void DrawBuyerPopup(int idx, string playerEntry)
    {
        using var popup = ImRaii.Popup($"##RaffleBuyerPopup{idx}");
        if (!popup.Success) return;
        var buyer = this.service.GetPlayerBuyer(playerEntry);
        ImGui.TextColored(EmporiumNeonTheme.RaffleTeal, $"Buyer for {PlayerInfoService.StripWorld(playerEntry)}");
        ImGui.Separator();
        ImGui.Spacing();
        if (!string.IsNullOrEmpty(buyer))
        {
            ImGui.TextDisabled("Buyer:");
            ImGui.SameLine();
            ImGui.TextColored(EmporiumNeonTheme.SuccessMint, buyer);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Clear", $"##RaffleClearBuyer{idx}"))
            {
                this.service.ClearPlayerBuyer(playerEntry);
                ImGui.CloseCurrentPopup();
            }
            ImGui.Spacing();
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", $"##RaffleBuyerTell{idx}"))
            {
                RequestGilBuyer.Execute(buyer, playerEntry, this.config, this.chatQueue);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade (Buyer)", $"##RaffleBuyerTrade{idx}"))
            {
                SendTradeRequest.Execute(buyer, this.chatQueue);
                ImGui.CloseCurrentPopup();
            }
        }
        else
        {
            var (charName, charWorld) = GetCurrentTarget();
            if (!string.IsNullOrEmpty(charName))
            {
                ImGui.TextDisabled("Targeted:");
                ImGui.SameLine();
                ImGui.TextUnformatted(charName);
                ImGui.Spacing();
                if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Set as Buyer", $"##RaffleSetBuyer{idx}"))
                {
                    var full = string.IsNullOrEmpty(charWorld) ? charName : $"{charName}@{charWorld}";
                    this.service.SetPlayerBuyer(playerEntry, full);
                }
            }
            else
            {
                ImGui.TextDisabled("Target a player in-game to set them as the buyer.");
            }
        }
    }

    private void DrawAddPlayerCombo()
    {
        var nearby  = PlayerInfoService.GetNearbySorted();
        var preview = nearby.Count == 0 ? "No players nearby" : "Add player...";
        ImGui.SetNextItemWidth(-1f);
        using var combo = ImRaii.Combo("##RaffleNearbyAdd", preview, ImGuiComboFlags.HeightLarge);
        if (!combo.Success) return;
        if (nearby.Count == 0) { ImGui.TextDisabled("No players in the area."); return; }
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText("##RaffleNearbyFilter", ref this.comboFilter, 64);
        var any = false;
        foreach (var player in nearby)
        {
            if (!player.Contains(this.comboFilter, StringComparison.OrdinalIgnoreCase)) continue;
            any = true;
            if (IsAlreadyRegistered(player))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EmporiumNeonTheme.RaffleTeal);
                ImGui.Selectable($"[Added]  {player}", false, ImGuiSelectableFlags.Disabled);
                ImGui.PopStyleColor();
            }
            else if (ImGui.Selectable(player))
            {
                this.service.AddPlayer(player);
                this.comboFilter = string.Empty;
                ImGui.CloseCurrentPopup();
            }
        }
        if (!any) ImGui.TextDisabled("No matches.");
    }

    private bool IsAlreadyRegistered(string nearbyEntry)
    {
        var state = this.service.GetState();
        if (state == null) return false;
        var nearbyName = PlayerInfoService.StripWorld(nearbyEntry);
        foreach (var e in state.Entries)
            if (PlayerInfoService.StripWorld(e.PlayerName).Equals(nearbyName, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static (string Name, string World) GetCurrentTarget()
    {
        var target = MiniGamesEmporium.TargetManager.Target;
        if (target is Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter pc)
            return (pc.Name.TextValue, pc.HomeWorld.Value.Name.ToString());
        return (string.Empty, string.Empty);
    }

    private void DrawSidePane(RaffleState state)
    {
        if (DrawCloseCountdown(state))
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }
        DrawAnnounceButtons();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawDrawSection(state);
    }

    private static bool DrawCloseCountdown(RaffleState state)
    {
        if (!state.HasCloseTime) return false;
        var timeLeft = RaffleTimeUtil.FormatTimeLeft(state.CloseAtUtc);
        var colour   = timeLeft == "Closed" ? EmporiumNeonTheme.WarnAmber : EmporiumNeonTheme.RaffleTeal;
        ImGui.TextDisabled($"Closes at {RaffleTimeUtil.FormatCloseLabel(state.CloseHour, state.CloseMinute)} ST -");
        ImGui.SameLine();
        ImGui.TextColored(colour, timeLeft);
        return true;
    }

    private void DrawBottomStats(RaffleState state, bool showKept)
    {
        var ticketsSold = this.service.ComputeTicketsSold();
        var totalPot    = this.service.ComputeTotalPot();
        using var child = ImRaii.Child("##RaffleStatsPanel", new Vector2(-1f, GetStatsHeight(showKept)), true);
        if (!child.Success) return;
        using var table = ImRaii.Table("##RaffleStatsTable", 3, ImGuiTableFlags.None, new Vector2(-1f, 0f));
        if (!table.Success) return;
        ImGui.TableSetupColumn("##RStatsLabel",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##RStatsAction", ImGuiTableColumnFlags.WidthFixed, 130f);
        ImGui.TableSetupColumn("##RStatsValue",  ImGuiTableColumnFlags.WidthFixed, 180f);
        DrawTotalPotRow(totalPot);
        DrawRow("Boosted Pot",  $"{state.BoostedPotAtStart:N0} Gil", EmporiumNeonTheme.WinGold);
        if (showKept)
            DrawRow("Kept from Trades", $"{this.service.ComputeTradesHeldBack():N0} Gil", EmporiumNeonTheme.WarnAmber);
        DrawRow("Ticket Cost",  state.TicketCostAtStart == 0 ? "Free" : $"{state.TicketCostAtStart:N0} Gil", EmporiumNeonTheme.RaffleTeal);
        DrawRow("Players",      this.service.ComputePlayersWithTickets().ToString(), EmporiumNeonTheme.NeonMagenta);
        DrawRow("Tickets Sold", ticketsSold.ToString(), EmporiumNeonTheme.NeonCyan);
        DrawDonationRow();
    }

    private void DrawTotalPotRow(long totalPot)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled("Total Pot");
        ImGui.TableSetColumnIndex(1);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4f, 0f));
        ImGui.PushStyleColor(ImGuiCol.Button,        GoldBtn);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GoldBtnHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GoldBtnActive);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Pot", "##RaffleAnnPot");
        ImGui.PopStyleColor(3);
        ImGui.PopStyleVar();
        if (clicked)
            AnnouncePot.Execute(this.config, this.chatQueue, totalPot);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{totalPot:N0} Gil");
    }

    private void DrawDonationRow()
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled("Adjust Pot (Gil)");
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputInt("##RaffleDonation", ref this.donationInput, 0, 0);
        ImGui.TableSetColumnIndex(2);
        using (UIHelper.PushGreenButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Add", "##RaffleAddDon") && this.donationInput > 0)
            {
                AddDonation.Execute(this.config, this.historyService, this.donationInput);
                this.donationInput = 0;
            }
        ImGui.SameLine();
        using (UIHelper.PushRedButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Minus, "Remove", "##RaffleRemDon") && this.donationInput > 0)
            {
                RemoveDonation.Execute(this.config, this.historyService, this.donationInput);
                this.donationInput = 0;
            }
    }

    private static void DrawRow(string label, string value, Vector4 valueColour)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(2);
        ImGui.TextColored(valueColour, value);
    }

    private void DrawAnnounceButtons()
    {
        ImGui.TextColored(EmporiumNeonTheme.RaffleTeal, "Shouts");
        ImGui.Spacing();
        using (Btn(TealBtn, TealBtnHovered, TealBtnActive))
            if (UIHelper.IconTextButton(FontAwesomeIcon.TicketAlt, "Tickets Sold", "##RaffleAnnTickets"))
                AnnounceTicketsSold.Execute(this.config, this.chatQueue);
        ImGui.SameLine();
        using (UIHelper.PushBlueButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Clock, "Closing Time", "##RaffleAnnClosing"))
                AnnounceClosingTime.Execute(this.config, this.chatQueue);
        if (this.config.Raffle.AutoJoinKeyword)
            using (Btn(MagentaBtn, MagentaBtnHover, MagentaBtnActive))
                if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Join Reminder", "##RaffleAnnJoin"))
                    AnnounceJoinReminder.Execute(this.config, this.chatQueue);
    }

    private void DrawDrawSection(RaffleState state)
    {
        ImGui.TextColored(EmporiumNeonTheme.RaffleTeal, "Draw Winner");
        ImGui.Spacing();
        if (state.HasDrawn)
        {
            DrawResult(state);
            return;
        }
        if (state.IsDrawArmed)
        {
            DrawArmed(state);
            return;
        }
        if (!this.service.CanDraw())
        {
            ImGui.TextDisabled("Sell at least one ticket before drawing.");
            return;
        }
        using (UIHelper.PushGreenButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Lock, "Raffle Closed", "##RaffleArm"))
            {
                AnnounceRaffleClosed.Execute(this.config, this.chatQueue);
                this.service.ArmDraw();
            }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Announces closed in chat and arms the draw.");
    }

    private void DrawArmed(RaffleState state)
    {
        using (UIHelper.PushBlueButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Dice, $"Roll {state.HighestNumber}", "##RaffleRoll"))
                this.chatQueue.Enqueue($"/random {state.HighestNumber}");
        ImGui.Spacing();
        using (UIHelper.PushRedButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel Draw", "##RaffleCancelArm"))
                this.service.ClearDraw();
        ImGui.Spacing();
        ImGui.TextDisabled("Or enter the winning number manually:");
        ImGui.SetNextItemWidth(140f);
        ImGui.InputInt("##RaffleManualDraw", ref this.manualDrawInput, 1, 1);
        ImGui.SameLine();
        using (UIHelper.PushGreenButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Check, "Set", "##RaffleManualSet") && this.manualDrawInput > 0)
                this.service.SetWinnerManually(this.manualDrawInput);
    }

    private void DrawResult(RaffleState state)
    {
        var number = state.WinningNumber ?? 0;
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"Number {number} was not sold - no winner.");
        ImGui.TextDisabled("Clear and draw again.");
        ImGui.Spacing();
        using (UIHelper.PushRedButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Redo, "Clear / Re-draw", "##RaffleClearDraw"))
                this.service.ClearDraw();
    }

    private void DrawRaffleComplete(RaffleState state)
    {
        var avail      = ImGui.GetContentRegionAvail().X;
        var startX     = ImGui.GetCursorPosX();
        var winnerName = PlayerInfoService.StripWorld(state.WinnerName ?? string.Empty);
        var number     = state.WinningNumber ?? 0;

        ImGui.SetWindowFontScale(1.6f);
        var nameW = ImGui.CalcTextSize(winnerName).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - nameW) * 0.5f));
        ImGui.TextColored(GoldColour, winnerName);
        ImGui.SetWindowFontScale(1.0f);

        const string subtitle = "RAFFLE WINNER!";
        var subtitleW = ImGui.CalcTextSize(subtitle).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - subtitleW) * 0.5f));
        ImGui.TextColored(GoldColour, subtitle);

        var numberText = $"Winning number {number}";
        var numberW    = ImGui.CalcTextSize(numberText).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - numberW) * 0.5f));
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, numberText);

        ImGui.Spacing();

        var trophySide = TrophySide * ImGuiHelpers.GlobalScale;
        var tex = this.trophyTexture?.GetWrapOrDefault();
        if (tex != null)
        {
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - trophySide) * 0.5f));
            ImGui.Image(tex.Handle, new Vector2(trophySide, trophySide));
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var pot       = this.service.ComputeTotalPot();
        var paid      = state.WinnerPayoutGil;
        var remaining = Math.Max(0L, pot - paid);

        var labelColW = MathF.Max(ImGui.CalcTextSize("Pot:").X, MathF.Max(ImGui.CalcTextSize("Traded:").X, ImGui.CalcTextSize("Remaining:").X));
        var valueColW = MathF.Max(ImGui.CalcTextSize($"{pot:N0} Gil").X, MathF.Max(ImGui.CalcTextSize($"{paid:N0} Gil").X, ImGui.CalcTextSize($"{remaining:N0} Gil").X));
        var spacing   = ImGui.GetStyle().ItemSpacing.X;
        var blockW    = labelColW + spacing + valueColW;
        var rowX      = startX + MathF.Max(0f, (avail - blockW) * 0.5f);
        var valueX    = rowX + labelColW + spacing;

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(GoldColour, "Pot:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(GoldColour, $"{pot:N0} Gil");

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, "Traded:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(EmporiumNeonTheme.SuccessMint, $"{paid:N0} Gil");

        ImGui.SetCursorPosX(rowX);
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Remaining:");
        ImGui.SameLine(valueX);
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"{remaining:N0} Gil");

        ImGui.Spacing();

        if (!this.config.Raffle.Chat.AutoAnnounceWinner)
        {
            var annBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Bullhorn, "Announce Winner").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - annBtnW) * 0.5f));
            using (UIHelper.PushYellowButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.Bullhorn, "Announce Winner", "##RaffleWinAnn"))
                    AnnounceWinner.Execute(this.config, this.chatQueue, state.WinnerName!, number, pot);
            ImGui.Spacing();
        }

        var tradeBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Coins, "Trade Winner").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - tradeBtnW) * 0.5f));
        using (UIHelper.PushAmberButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade Winner", "##RaffleWinTrade"))
                SendTradeRequest.Execute(winnerName, this.chatQueue);

        ImGui.Spacing();

        DrawRaffleAutoPayoutButton(winnerName, remaining, avail, startX);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var progress   = pot > 0 ? MathF.Min(1f, (float)paid / pot) : 1f;
        var pctOverlay = $"{progress * 100f:F0}% paid out";
        ImGui.SetCursorPosX(startX);
        ImGui.ProgressBar(progress, new Vector2(avail, ImGui.GetFrameHeight()), pctOverlay);

        ImGui.Spacing();
        var redrawW = UIHelper.CalcButtonSize(FontAwesomeIcon.Redo, "Clear / Re-draw").X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - redrawW) * 0.5f));
        using (UIHelper.PushRedButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Redo, "Clear / Re-draw", "##RaffleWinRedraw"))
                this.service.ClearDraw();
    }

    private void DrawRaffleAutoPayoutButton(string winnerName, long remaining, float avail, float startX)
    {
        if (this.autoPayoutService.IsRunningFor(winnerName))
        {
            var stopBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.Stop, "Stop Auto Payout").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - stopBtnW) * 0.5f));
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Auto Payout", "##RaffleWinStopPayout"))
                this.autoPayoutService.Stop();
        }
        else
        {
            using var disabled = ImRaii.Disabled(remaining <= 0 || this.autoPayoutService.IsRunning);
            var autoBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.MoneyBillWave, "Auto Payout").X;
            ImGui.SetCursorPosX(startX + MathF.Max(0f, (avail - autoBtnW) * 0.5f));
            using var green = UIHelper.PushGreenButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.MoneyBillWave, "Auto Payout", "##RaffleWinAutoPayout"))
                this.autoPayoutService.Start(winnerName, this.service.GetWinnerRemaining, this.service.IsSessionActive);
        }
    }

    private static ImRaii.ColorDisposable Btn(Vector4 normal, Vector4 hovered, Vector4 active) =>
        new ImRaii.ColorDisposable()
            .Push(ImGuiCol.Button,        normal)
            .Push(ImGuiCol.ButtonHovered, hovered)
            .Push(ImGuiCol.ButtonActive,  active);
}
