using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using MiniGamesEmporium.Actions;
using MiniGamesEmporium.Services;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Actions;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using MiniGamesEmporium.Games.DeathrollTournament.Utility;
using MiniGamesEmporium.Games.DeathrollTournament.Webview;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

/// <summary>Draws the Deathroll Tournament registration and bracket game view.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollBracketTab
{
    private static readonly Vector4 PinkButton        = new(0.72f, 0.06f, 0.36f, 1f);
    private static readonly Vector4 PinkButtonHovered = new(0.90f, 0.10f, 0.48f, 1f);
    private static readonly Vector4 PinkButtonActive  = new(0.55f, 0.04f, 0.26f, 1f);
    private static readonly Vector4 GreenButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);
    private static readonly Vector4 YellowButton        = new(0.45f, 0.35f, 0.02f, 1f);
    private static readonly Vector4 YellowButtonHovered = new(0.60f, 0.48f, 0.03f, 1f);
    private static readonly Vector4 YellowButtonActive  = new(0.32f, 0.25f, 0.01f, 1f);
    private static readonly Vector4 OrangeButton        = new(0.60f, 0.25f, 0.02f, 1f);
    private static readonly Vector4 OrangeButtonHovered = new(0.80f, 0.35f, 0.03f, 1f);
    private static readonly Vector4 OrangeButtonActive  = new(0.45f, 0.18f, 0.01f, 1f);
    private static readonly Vector4 CurrentMatchBg    = new(0.28f, 0.22f, 0.02f, 1f);
    private static readonly Vector4 ResolvedMatchBg   = new(0.06f, 0.10f, 0.06f, 1f);
    private static readonly Vector4 GoldColour        = new(1f, 0.84f, 0f, 1f);
    private static readonly Vector4 CardAccent       = EmporiumNeonTheme.DeathrollTournamentPink;
    private static readonly Vector4 CardTitle        = EmporiumNeonTheme.Secondary(CardAccent);
    private const float RightPaneW  = 300f;
    private const float BestOfPaneW = 240f;
    private const float TrophySide  = 140f;
    private static readonly Vector4 TagAccent = EmporiumNeonTheme.DeathrollTournamentPink;

    private readonly PluginConfiguration config;
    private readonly DeathrollTournamentService deathrollService;
    private readonly DeathrollBettingService bettingService;
    private readonly ChatQueueService chatQueue;
    private readonly AutoPayoutService autoPayoutService;
    private readonly DrtWebviewService webviewService;
    private readonly ISharedImmediateTexture? _trophyTexture;
    private readonly ThemedCard card = new();
    private static string comboFilter = string.Empty;
    private string swapFilter = string.Empty;
    private string preSignUpInput = string.Empty;
    private string linkFilter = string.Empty;
    private bool openUnpaidModal = false;
    private List<string> unpaidModalPlayers = new();
    private bool openUnlinkedModal = false;
    private List<string> unlinkedModalPlayers = new();
    private bool openUnresolvedBetsModal = false;
    private List<DeathrollBet> unresolvedBetsModalBets = new();
    private bool viewingBracketAfterWin = false;

    public DeathrollBracketTab(PluginConfiguration config, DeathrollTournamentService deathrollService, DeathrollBettingService bettingService, ChatQueueService chatQueue, AutoPayoutService autoPayoutService, DrtWebviewService webviewService)
    {
        this.config            = config;
        this.deathrollService  = deathrollService;
        this.bettingService    = bettingService;
        this.chatQueue         = chatQueue;
        this.autoPayoutService = autoPayoutService;
        this.webviewService    = webviewService;
        var path = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "trophy.png");
        if (File.Exists(path))
            _trophyTexture = MiniGamesEmporium.TextureProvider.GetFromFile(path);
    }

    public void Draw(bool skipLeadingSpacing = false, float reserveBottom = 0f, Action? drawStatsInline = null, Action? drawShoutsInline = null)
    {
        if (!skipLeadingSpacing) ImGui.Spacing();
        if (!this.deathrollService.HasActiveTournament())
        {
            DrawPreTournamentSetup(reserveBottom, drawStatsInline, drawShoutsInline);
            DrawUnpaidPlayersModal();
            DrawUnlinkedPlayersModal();
            DrawUnresolvedBetsModal();
            return;
        }
        var state = this.config.DeathrollTournamentSession!;
        if (state.TournamentWinner == null)
            this.viewingBracketAfterWin = false;
        else if (!this.viewingBracketAfterWin)
        {
            drawShoutsInline?.Invoke();
            DrawTournamentComplete(state);
            return;
        }
        var fullH = MathF.Max(100f, ImGui.GetContentRegionAvail().Y);
        using var split = ImRaii.Table("##DRSplit_v2",
            CollapsiblePanels.SideColumnCount(PanelKeys.DeathrollTracker),
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1f, fullH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##DRBracketCol", ImGuiTableColumnFlags.WidthStretch);
        CollapsiblePanels.SetupSideColumns(PanelKeys.DeathrollTracker, "##DRTracker", RightPaneW * ImGuiHelpers.GlobalScale);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        drawShoutsInline?.Invoke();
        var cellH   = ImGui.GetContentRegionAvail().Y;
        var colTopY = ImGui.GetCursorPosY();
        DrawBracketPane(state, MathF.Max(100f, cellH - reserveBottom));
        if (drawStatsInline != null)
        {
            var targetY = colTopY + cellH - reserveBottom;
            if (targetY > ImGui.GetCursorPosY())
                ImGui.SetCursorPosY(targetY);
            drawStatsInline();
        }

        ImGui.TableSetColumnIndex(1);
        if (!CollapsiblePanels.DrawSideTag(PanelKeys.DeathrollTracker, "##DRTrackerTag", TagAccent, "the tracker"))
            return;
        ImGui.TableSetColumnIndex(2);
        DrawTrackerPane(state, ImGui.GetContentRegionAvail().Y);
    }

    private void DrawPreTournamentSetup(float reserveBottom, Action? drawStatsInline, Action? drawShoutsInline)
    {
        var splitH = MathF.Max(60f, ImGui.GetContentRegionAvail().Y);
        using var split = ImRaii.Table("##DRPreSplit_v2",
            CollapsiblePanels.SideColumnCount(PanelKeys.DeathrollBestOf),
            ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV,
            new Vector2(-1f, splitH));
        if (!split.Success) return;
        ImGui.TableSetupColumn("##DRPlayerCol", ImGuiTableColumnFlags.WidthStretch);
        CollapsiblePanels.SetupSideColumns(PanelKeys.DeathrollBestOf, "##DRBestOf", BestOfPaneW * ImGuiHelpers.GlobalScale);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        drawShoutsInline?.Invoke();
        var cellH       = ImGui.GetContentRegionAvail().Y;
        var playerPaneH = MathF.Max(24f, cellH - reserveBottom);
        using (var playerPane = ImRaii.Child("##DRPlayerPane", new Vector2(-1f, playerPaneH), false, ImGuiWindowFlags.NoScrollbar))
            if (playerPane.Success) DrawPlayerList();
        drawStatsInline?.Invoke();

        ImGui.TableSetColumnIndex(1);
        if (!CollapsiblePanels.DrawSideTag(PanelKeys.DeathrollBestOf, "##DRBestOfTag", TagAccent, "the best of panel"))
            return;
        ImGui.TableSetColumnIndex(2);
        var boH = ImGui.GetContentRegionAvail().Y;
        using var boPane = ImRaii.Child("##DRBestOfPane", new Vector2(-1f, boH), false, ImGuiWindowFlags.NoScrollbar);
        if (boPane.Success) DrawBestOfPaneBody();
    }

    private void DrawBestOfPaneBody()
    {
        DrawBestOfSettings();
        var footerH   = 1f + ImGui.GetStyle().ItemSpacing.Y * 4f + ImGui.GetFrameHeight() + ImGui.GetTextLineHeightWithSpacing() * 3f;
        var remaining = ImGui.GetContentRegionAvail().Y - footerH;
        if (remaining > 0f)
            ImGui.Dummy(new Vector2(0f, remaining));
        ImGui.Separator();
        ImGui.Spacing();
        DrawStartTournamentButton();
    }

    private void DrawPlayerList()
    {
        var list            = this.config.DeathrollTournament.RegisteredPlayers;
        var paidCount       = this.config.DeathrollTournament.PaidPlayers.Count;
        var unverifiedCount = this.deathrollService.GetUnverifiedCount();
        var availH          = ImGui.GetContentRegionAvail().Y;
        var startY          = ImGui.GetCursorPosY();
        ImGui.TextColored(new Vector4(1f, 0.20f, 0.60f, 1f), $"Players ({paidCount} / {list.Count} paid)");
        if (unverifiedCount > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"-- {unverifiedCount} unlinked");
        }
        ImGui.Spacing();
        DrawAddPlayerCombo();
        ImGui.SameLine();
        using (UIHelper.PushButtonColours(
            EmporiumNeonTheme.DeathrollTournamentPink,
            new Vector4(1f, 0.45f, 0.75f, 1f),
            new Vector4(0.72f, 0.10f, 0.40f, 1f)))
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Random, "Shuffle", "##DRShuffle"))
                this.deathrollService.ShufflePlayers();
        }
        DrawPreSignUpInput();
        DrawWebRequests();
        ImGui.Spacing();
        var tableH = MathF.Max(24f, availH - (ImGui.GetCursorPosY() - startY));
        if (list.Count == 0)
        {
            ImGui.TextDisabled("No players added yet.");
            return;
        }
        var spacing     = ImGui.GetStyle().ItemSpacing.X;
        var arrowColW   = UIHelper.CalcButtonSize(FontAwesomeIcon.ArrowUp, "").X * 2f + spacing;
        var actColW     = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request Gil").X
                        + UIHelper.CalcButtonSize(FontAwesomeIcon.Coins,       "Trade").X
                        + UIHelper.CalcButtonSize(FontAwesomeIcon.Times,       "Mark as Unpaid").X
                        + UIHelper.CalcButtonSize(FontAwesomeIcon.Trash,       "Remove Player").X
                        + UIHelper.CalcButtonSize(FontAwesomeIcon.Gift,        "").X
                        + spacing * 4f;
        int removeIdx = -1, togglePaidIdx = -1, targetIdx = -1, moveUpIdx = -1, moveDownIdx = -1;
        using var table = ImRaii.Table("##DRPlayerTable", 4,
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(-1f, tableH));
        if (!table.Success) return;
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("##DRArrowCl", ImGuiTableColumnFlags.WidthFixed,   arrowColW);
        ImGui.TableSetupColumn("#",           ImGuiTableColumnFlags.WidthFixed,   26f);
        ImGui.TableSetupColumn("Name",        ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##DRActCl",   ImGuiTableColumnFlags.WidthFixed,   actColW);
        ImGui.TableHeadersRow();
        for (var i = 0; i < list.Count; i++)
        {
            ImGui.TableNextRow();
            DrawPlayerTableRow(i, list[i], list.Count, ref removeIdx, ref togglePaidIdx, ref targetIdx, ref moveUpIdx, ref moveDownIdx);
        }
        if (targetIdx     >= 0) SendTradeRequest.Execute(PlayerInfoService.StripWorld(list[targetIdx]), this.chatQueue);
        if (togglePaidIdx >= 0) this.deathrollService.TogglePaid(list[togglePaidIdx]);
        if (moveUpIdx     >= 0) this.deathrollService.MovePlayerUp(moveUpIdx);
        if (moveDownIdx   >= 0) this.deathrollService.MovePlayerDown(moveDownIdx);
        if (removeIdx     >= 0) this.deathrollService.RemovePlayer(removeIdx);
    }

    private void DrawAddPlayerCombo()
    {
        var nearby  = PlayerInfoService.GetNearbySorted();
        var preview = nearby.Count == 0 ? "No players nearby" : "Add player...";
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - UIHelper.CalcButtonSize(FontAwesomeIcon.Random, "Shuffle").X - ImGui.GetStyle().ItemSpacing.X);
        using var combo = ImRaii.Combo("##DRNearbyAdd", preview, ImGuiComboFlags.HeightLarge);
        if (!combo.Success) return;
        if (nearby.Count == 0) { ImGui.TextDisabled("No players in the area."); return; }
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere();
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText("##DRNearbyFilter", ref comboFilter, 64);
        var any = false;
        foreach (var player in nearby)
        {
            if (!player.Contains(comboFilter, StringComparison.OrdinalIgnoreCase)) continue;
            any = true;
            var alreadyAdded = IsAlreadyRegistered(player);
            if (alreadyAdded)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, EmporiumNeonTheme.DeathrollTournamentPink);
                ImGui.Selectable($"[Added]  {player}", false, ImGuiSelectableFlags.Disabled);
                ImGui.PopStyleColor();
            }
            else
            {
                if (!ImGui.Selectable(player)) continue;
                this.deathrollService.AddPlayer(player);
                comboFilter = string.Empty;
                ImGui.CloseCurrentPopup();
            }
        }
        if (!any) ImGui.TextDisabled("No matches.");
    }

    private void DrawPlayerTableRow(int idx, string entry, int count, ref int removeIdx, ref int togglePaidIdx, ref int targetIdx, ref int moveUpIdx, ref int moveDownIdx)
    {
        var displayName = PlayerInfoService.StripWorld(entry);
        var isPaid      = this.deathrollService.IsPaid(entry);
        var isVerified  = this.deathrollService.IsPlayerVerified(entry);
        var green       = new Vector4(0.22f, 0.82f, 0.32f, 1f);
        if (isPaid)
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.05f, 0.25f, 0.10f, 1f)));
        ImGui.TableSetColumnIndex(0);
        using (ImRaii.Disabled(idx == 0))
            if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowUp, "", $"##DRUp{idx}"))
                moveUpIdx = idx;
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Move up");
        ImGui.SameLine();
        using (ImRaii.Disabled(idx == count - 1))
            if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowDown, "", $"##DRDown{idx}"))
                moveDownIdx = idx;
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Move down");
        ImGui.TableSetColumnIndex(1);
        ImGui.TextDisabled($"{idx + 1}");
        ImGui.TableSetColumnIndex(2);
        var nameColour = !isVerified ? EmporiumNeonTheme.WarnAmber
                       : isPaid      ? green
                       :               new Vector4(0.94f, 0.92f, 0.98f, 1f);
        ImGui.TextColored(nameColour, displayName);
        if (!isVerified)
        {
            ImGui.SameLine(0f, 4f);
            ImGui.TextDisabled("[unlinked]");
        }
        ImGui.TableSetColumnIndex(3);
        var reqGilW  = UIHelper.CalcButtonSize(FontAwesomeIcon.CommentDots, "Request Gil").X;
        var tradeW   = UIHelper.CalcButtonSize(FontAwesomeIcon.Coins, "Trade").X;
        if (!isVerified)
        {
            var linkNaturalW = UIHelper.CalcButtonSize(FontAwesomeIcon.Link, "Link Player").X;
            var padW         = MathF.Max(0f, reqGilW + tradeW - linkNaturalW);
            if (padW > 0f) { ImGui.Dummy(new Vector2(padW, ImGui.GetFrameHeight())); ImGui.SameLine(); }
            ImGui.PushStyleColor(ImGuiCol.Button,        YellowButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, YellowButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  YellowButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Link, "Link Player", $"##DRLink{idx}"))
                ImGui.OpenPopup($"##DRLinkPopup{idx}");
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Link to a nearby player so rolls can be tracked");
        }
        else if (!isPaid)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.10f, 0.36f, 0.72f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.15f, 0.50f, 0.90f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.08f, 0.28f, 0.55f, 1f));
            if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil", $"##DRReqGil{idx}"))
                RequestGil.Execute(entry, this.config, this.chatQueue);
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Send a /tell requesting the entry fee");
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.52f, 0.40f, 0.02f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.72f, 0.58f, 0.03f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.38f, 0.28f, 0.01f, 1f));
            if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade", $"##DRTrade{idx}"))
                targetIdx = idx;
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Send trade request");
        }
        else
        {
            ImGui.Dummy(new Vector2(reqGilW, ImGui.GetFrameHeight()));
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(tradeW, ImGui.GetFrameHeight()));
        }
        ImGui.SameLine();
        var toggleBtnW = MathF.Max(
            UIHelper.CalcButtonSize(FontAwesomeIcon.Check, "Mark as Paid").X,
            UIHelper.CalcButtonSize(FontAwesomeIcon.Times, "Mark as Unpaid").X);
        if (isPaid)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.EmporiumPurpleActive);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.58f, 0.16f, 0.80f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  EmporiumNeonTheme.EmporiumPurple);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Mark as Unpaid", $"##DRPaid{idx}", toggleBtnW))
                togglePaidIdx = idx;
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to unmark as paid");
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.14f, 0.48f, 0.18f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.20f, 0.64f, 0.26f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.10f, 0.36f, 0.14f, 1f));
            if (UIHelper.IconTextButton(FontAwesomeIcon.Check, "Mark as Paid", $"##DRPaid{idx}", toggleBtnW))
                togglePaidIdx = idx;
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Click to mark as paid");
        }
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.60f, 0.08f, 0.08f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.80f, 0.12f, 0.12f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.45f, 0.06f, 0.06f, 1f));
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Remove Player", $"##DRRem{idx}") && ImGui.GetIO().KeyCtrl)
            removeIdx = idx;
        ImGui.PopStyleColor(3);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hold Ctrl and click to remove player");
        ImGui.SameLine();
        var hasBuyer = !string.IsNullOrEmpty(this.deathrollService.GetPlayerBuyer(entry));
        if (hasBuyer)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.SuccessMint with { W = 0.8f });
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, EmporiumNeonTheme.SuccessMint);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  EmporiumNeonTheme.SuccessMint with { W = 0.6f });
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.DeathrollTournamentPink);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.45f, 0.75f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.72f, 0.10f, 0.40f, 1f));
        }
        if (UIHelper.IconTextButton(FontAwesomeIcon.Gift, "", $"##DRBuyerBtn{idx}"))
            ImGui.OpenPopup($"##DRBuyerPopup{idx}");
        ImGui.PopStyleColor(3);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(hasBuyer ? $"Buyer: {this.deathrollService.GetPlayerBuyer(entry)}" : "Set a buyer to pay for this player");
        DrawLinkPlayerPopup(idx, entry);
        using var popup = ImRaii.Popup($"##DRBuyerPopup{idx}");
        if (popup.Success)
        {
            var buyer = this.deathrollService.GetPlayerBuyer(entry);
            ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, $"Buyer for {PlayerInfoService.StripWorld(entry)}");
            ImGui.Separator();
            ImGui.Spacing();
            if (!string.IsNullOrEmpty(buyer))
            {
                ImGui.TextDisabled("Buyer:");
                ImGui.SameLine();
                ImGui.TextColored(EmporiumNeonTheme.SuccessMint, buyer);
                ImGui.SameLine();
                if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Clear", $"##DRClearBuyer{idx}"))
                {
                    this.deathrollService.ClearPlayerBuyer(entry);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.Spacing();
                if (UIHelper.IconTextButton(FontAwesomeIcon.CommentDots, "Request Gil (Buyer)", $"##DRBuyerTell{idx}"))
                {
                    RequestGilBuyer.Execute(buyer, entry, this.config, this.chatQueue);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade (Buyer)", $"##DRBuyerTrade{idx}"))
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
                    if (UIHelper.IconTextButton(FontAwesomeIcon.UserCheck, "Set as Buyer", $"##DRSetBuyer{idx}"))
                    {
                        var fullBuyerName = string.IsNullOrEmpty(charWorld) ? charName : $"{charName}@{charWorld}";
                        this.deathrollService.SetPlayerBuyer(entry, fullBuyerName);
                    }
                }
                else
                {
                    ImGui.TextDisabled("Target a player in-game to set them as the buyer.");
                }
            }
        }
    }

    private bool IsAlreadyRegistered(string nearbyEntry)
    {
        var nearbyName = PlayerInfoService.StripWorld(nearbyEntry);
        var list = this.config.DeathrollTournament.RegisteredPlayers;
        foreach (var p in list)
        {
            var pName = PlayerInfoService.StripWorld(p);
            if (pName.Equals(nearbyName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void DrawBestOfSettings()
    {
        var players    = this.config.DeathrollTournament.RegisteredPlayers.Count;
        var roundCount = players >= 2 ? ComputeRoundCount(players) : 0;
        if (roundCount == 0)
        {
            ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
            ImGui.TextDisabled("Add at least 2 players to configure round settings.");
            ImGui.PopTextWrapPos();
            return;
        }
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Best-of per Round");
        ImGui.Spacing();
        var list     = this.config.DeathrollTournament.BestOfPerRound;
        var prevSize = list.Count;
        while (list.Count < roundCount) list.Add(1);
        while (list.Count > roundCount) list.RemoveAt(list.Count - 1);
        if (list.Count != prevSize) this.config.Save();
        ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
        for (var i = 0; i < roundCount; i++)
        {
            var val   = list[i];
            var label = i == roundCount - 1 ? "Final" : $"Round {i + 1}";
            ImGui.SetNextItemWidth(120f);
            if (ImGui.InputInt($"{label}##DRBO{i}", ref val, 2, 2))
            {
                list[i] = Math.Max(1, val % 2 == 0 ? val + 1 : val);
                this.config.Save();
            }
            ImGui.TextDisabled($"Best of {list[i]}  (need {list[i] / 2 + 1} win(s))");
        }
        ImGui.PopTextWrapPos();
    }

    private void DrawStartTournamentButton()
    {
        var paidCount      = this.config.DeathrollTournament.PaidPlayers.Count;
        var unverifiedCount = this.deathrollService.GetUnverifiedCount();
        var canStart       = paidCount >= 2;
        var btnW           = UIHelper.CalcButtonSize(FontAwesomeIcon.Play, "Start Tournament").X;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - btnW) * 0.5f);
        ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
        using var disabled = ImRaii.Disabled(!canStart);
        var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Tournament", "##DRStartTournament");
        ImGui.PopStyleColor(3);
        if (clicked)
        {
            var unlinked = this.deathrollService.GetUnverifiedRegisteredPlayers();
            if (unlinked.Count > 0)
            {
                this.unlinkedModalPlayers = unlinked;
                this.openUnlinkedModal    = true;
            }
            else
            {
                var unpaid = this.deathrollService.GetUnpaidRegisteredPlayers();
                if (unpaid.Count > 0)
                {
                    this.unpaidModalPlayers = unpaid;
                    this.openUnpaidModal    = true;
                }
                else
                {
                    var unresolvedBets = this.bettingService.GetUnresolvedBets();
                    if (unresolvedBets.Count > 0)
                    {
                        this.unresolvedBetsModalBets = unresolvedBets;
                        this.openUnresolvedBetsModal = true;
                    }
                    else
                    {
                        this.deathrollService.StartTournament();
                    }
                }
            }
        }
        if (!canStart || unverifiedCount > 0)
        {
            ImGui.Spacing();
            ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
            if (!canStart)
                ImGui.TextDisabled("Mark at least 2 players as paid to start.");
            if (unverifiedCount > 0)
                ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"{unverifiedCount} player(s) must be linked before the tournament can start.");
            ImGui.PopTextWrapPos();
        }
    }

    private void DrawUnpaidPlayersModal()
    {
        if (this.openUnpaidModal)
        {
            ImGui.OpenPopup("Unpaid Players##DRUnpaidModal");
            this.openUnpaidModal = false;
        }
        using var modal = ImRaii.PopupModal("Unpaid Players##DRUnpaidModal", ImGuiWindowFlags.AlwaysAutoResize);
        if (!modal.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "The following players have not been marked as paid:");
        ImGui.Spacing();
        foreach (var name in this.unpaidModalPlayers)
            ImGui.BulletText(name);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        const float closeW = 80f;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - closeW) * 0.5f);
        if (ImGui.Button("Close##DRUnpaidClose", new Vector2(closeW, 0f)))
            ImGui.CloseCurrentPopup();
    }

    private void DrawUnlinkedPlayersModal()
    {
        if (this.openUnlinkedModal)
        {
            ImGui.OpenPopup("Unlinked Players##DRUnlinkedModal");
            this.openUnlinkedModal = false;
        }
        using var modal = ImRaii.PopupModal("Unlinked Players##DRUnlinkedModal", ImGuiWindowFlags.AlwaysAutoResize);
        if (!modal.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "The following players have not been linked to a character:");
        ImGui.Spacing();
        foreach (var name in this.unlinkedModalPlayers)
            ImGui.BulletText(name);
        ImGui.Spacing();
        ImGui.TextDisabled("Use the Link Player button on each entry to connect");
        ImGui.TextDisabled("them to a nearby character before starting.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        const float closeW = 80f;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - closeW) * 0.5f);
        if (ImGui.Button("Close##DRUnlinkedClose", new Vector2(closeW, 0f)))
            ImGui.CloseCurrentPopup();
    }

    private void DrawUnresolvedBetsModal()
    {
        if (this.openUnresolvedBetsModal)
        {
            ImGui.OpenPopup("Unresolved Bets##DRUnresolvedBetsModal");
            this.openUnresolvedBetsModal = false;
        }
        using var modal = ImRaii.PopupModal("Unresolved Bets##DRUnresolvedBetsModal", ImGuiWindowFlags.AlwaysAutoResize);
        if (!modal.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "The following bets are unpaid or unresolved and will not count toward the pot:");
        ImGui.Spacing();
        foreach (var bet in this.unresolvedBetsModalBets)
            ImGui.BulletText($"{PlayerInfoService.StripWorld(bet.BettorName)} -> {bet.TargetName}");
        ImGui.Spacing();
        ImGui.TextDisabled("Mark them as paid, fix the target, or remove them from the Betting tab before starting.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        const float closeW = 80f;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - closeW) * 0.5f);
        if (ImGui.Button("Close##DRUnresolvedBetsClose", new Vector2(closeW, 0f)))
            ImGui.CloseCurrentPopup();
    }

    private void DrawPreSignUpInput()
    {
        var addBtnW = UIHelper.CalcButtonSize(FontAwesomeIcon.UserPlus, "Add").X;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - addBtnW - ImGui.GetStyle().ItemSpacing.X);
        var submitted = ImGui.InputText("##DRPreSignUp", ref this.preSignUpInput, 64, ImGuiInputTextFlags.EnterReturnsTrue);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Enter a name to pre-sign-up a player who is not currently nearby");
        ImGui.SameLine();
        using (ImRaii.Disabled(string.IsNullOrWhiteSpace(this.preSignUpInput)))
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        new Vector4(0.45f, 0.35f, 0.02f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.62f, 0.50f, 0.03f, 1f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.32f, 0.25f, 0.01f, 1f));
            var clicked = UIHelper.IconTextButton(FontAwesomeIcon.UserPlus, "Add", "##DRAddPreSignUp");
            ImGui.PopStyleColor(3);
            if ((clicked || submitted) && !string.IsNullOrWhiteSpace(this.preSignUpInput))
            {
                this.deathrollService.AddPreSignupPlayer(this.preSignUpInput);
                this.preSignUpInput = string.Empty;
            }
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip("Pre-sign-up a player who is not currently nearby");
    }

    private void DrawWebRequests()
    {
        if (this.webviewService.SessionId == null) return;
        var pending = this.webviewService.PendingJoins;
        if (pending.Count == 0) return;

        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, $"Web Requests ({pending.Count})");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Join requests submitted from the website. Accepted names are added as unlinked players.");
        ImGui.Spacing();
        var acceptW = UIHelper.CalcButtonSize(FontAwesomeIcon.Check, "Accept").X;
        var rejectW = UIHelper.CalcButtonSize(FontAwesomeIcon.Times, "Reject").X;
        var actColW = acceptW + rejectW + ImGui.GetStyle().ItemSpacing.X * 2f;
        var tableH  = MathF.Min(pending.Count, 4) * (ImGui.GetFrameHeight() + ImGui.GetStyle().CellPadding.Y * 2f) + 4f;
        using var table = ImRaii.Table("##DRWebRequests", 2,
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(-1f, tableH));
        if (!table.Success) return;
        ImGui.TableSetupColumn("##DRWebReqName", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##DRWebReqAct",  ImGuiTableColumnFlags.WidthFixed, actColW);
        foreach (var request in pending.ToArray())
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(request.CharacterName);
            if (this.webviewService.NameCollides(request.CharacterName))
            {
                ImGui.SameLine();
                ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "[already registered]");
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("A player with this name is already on the roster. Accepting will not add a duplicate.");
            }
            ImGui.TableSetColumnIndex(1);
            ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
            var accepted = UIHelper.IconTextButton(FontAwesomeIcon.Check, "Accept", $"##DRWebAccept{request.Id}");
            ImGui.PopStyleColor(3);
            ImGui.SameLine();
            var rejected = UIHelper.IconTextButton(FontAwesomeIcon.Times, "Reject", $"##DRWebReject{request.Id}");
            if (accepted) this.webviewService.Accept(request);
            else if (rejected) this.webviewService.Reject(request);
        }
    }

    private void DrawLinkPlayerPopup(int idx, string entry)
    {
        using var popup = ImRaii.Popup($"##DRLinkPopup{idx}");
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, $"Link: {PlayerInfoService.StripWorld(entry)}");
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.IsWindowAppearing()) { this.linkFilter = string.Empty; ImGui.SetKeyboardFocusHere(); }
        ImGui.SetNextItemWidth(200f);
        ImGui.InputText("##DRLinkFilter", ref this.linkFilter, 64);
        ImGui.Spacing();
        var nearby = PlayerInfoService.GetNearbySorted();
        if (nearby.Count == 0) { ImGui.TextDisabled("No players nearby."); return; }
        var any = false;
        foreach (var player in nearby)
        {
            if (!player.Contains(this.linkFilter, StringComparison.OrdinalIgnoreCase)) continue;
            if (IsAlreadyVerifiedRegistered(player, entry)) continue;
            any = true;
            if (!ImGui.Selectable(player)) continue;
            this.deathrollService.LinkPlayer(idx, player);
            this.linkFilter = string.Empty;
            ImGui.CloseCurrentPopup();
        }
        if (!any) ImGui.TextDisabled("No matches.");
    }

    private bool IsAlreadyVerifiedRegistered(string nearbyEntry, string excludeEntry)
    {
        var nearbyName = PlayerInfoService.StripWorld(nearbyEntry);
        var excludeName = PlayerInfoService.StripWorld(excludeEntry);
        foreach (var p in this.config.DeathrollTournament.RegisteredPlayers)
        {
            var pName = PlayerInfoService.StripWorld(p);
            if (pName.Equals(excludeName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!this.deathrollService.IsPlayerVerified(p)) continue;
            if (pName.Equals(nearbyName, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void DrawBracketPane(DeathrollTournamentState state, float height)
    {
        var style      = ImGui.GetStyle();
        var roundCount = state.Rounds.Count;
        var firstCount = roundCount > 0 ? state.Rounds[0].Count : 1;
        var spacing    = style.ItemSpacing.Y;
        var paneW      = ImGui.GetContentRegionAvail().X;
        const float MinCardH = 80f;
        const float MinCardW = 120f;

        var headerH   = state.TournamentWinner != null ? ImGui.GetFrameHeight() + style.ItemSpacing.Y : 0f;
        var overhead  = headerH + spacing
                      + ImGui.GetTextLineHeightWithSpacing() + spacing * 2f
                      + style.WindowPadding.Y * 2f;
        var gapH      = MathF.Max(0, firstCount - 1) * spacing;
        var matchBoxH = MathF.Max(MinCardH, MathF.Min(200f, (height - overhead - gapH) / MathF.Max(1, firstCount)));
        var colW      = MathF.Max(MinCardW, paneW / MathF.Max(1, roundCount));

        using var pane = ImRaii.Child("##DRBracketPane", new Vector2(-1f, height), false, ImGuiWindowFlags.HorizontalScrollbar);
        if (!pane.Success) return;
        if (state.TournamentWinner != null)
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Trophy, "View Winner", "##DRViewWinner"))
                this.viewingBracketAfterWin = false;
        }
        ImGui.Spacing();
        DrawAllRounds(state, matchBoxH, colW);
    }

    private void DrawAllRounds(DeathrollTournamentState state, float matchBoxH, float colW)
    {
        var roundCount = state.Rounds.Count;
        using var table = ImRaii.Table("##DRRoundTable", roundCount, ImGuiTableFlags.BordersInnerV);
        if (!table.Success) return;
        for (var r = 0; r < roundCount; r++)
            ImGui.TableSetupColumn($"##DRRound{r}", ImGuiTableColumnFlags.WidthFixed, colW);
        ImGui.TableNextRow();
        var firstRoundCount = state.Rounds[0].Count;
        for (var r = 0; r < roundCount; r++)
        {
            ImGui.TableSetColumnIndex(r);
            DrawRoundColumn(state, r, firstRoundCount, matchBoxH);
        }
    }

    private void DrawRoundColumn(DeathrollTournamentState state, int roundIdx, int firstRoundMatchCount, float matchBoxH)
    {
        var matchBoxW  = MathF.Max(120f, ImGui.GetContentRegionAvail().X - 2f);
        var bestOf     = roundIdx < state.BestOfPerRound.Count ? state.BestOfPerRound[roundIdx] : 1;
        var roundLabel = roundIdx == state.Rounds.Count - 1 ? "Final" : $"Round {roundIdx + 1}";
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, $"{roundLabel} (BO{bestOf})");
        ImGui.Separator();
        ImGui.Spacing();
        var contentStartY = ImGui.GetCursorPosY();
        var slotH         = matchBoxH + ImGui.GetStyle().ItemSpacing.Y;
        var matches       = state.Rounds[roundIdx];
        var span          = matches.Count > 0 ? firstRoundMatchCount / matches.Count : 1;
        for (var m = 0; m < matches.Count; m++)
        {
            var isCurrent = roundIdx == state.CurrentRoundIndex && m == state.CurrentMatchIndex;
            ImGui.SetCursorPosY(contentStartY + m * span * slotH + (span * slotH - matchBoxH) * 0.5f);
            DrawMatchBox(state, matches[m], isCurrent, roundIdx, m, matchBoxW, matchBoxH);
        }
    }

    private void DrawMatchBox(DeathrollTournamentState state, BracketMatch match, bool isCurrent, int roundIdx, int matchIdx, float matchBoxW, float matchBoxH)
    {
        var bgColour = match.IsResolved ? ResolvedMatchBg : (isCurrent ? CurrentMatchBg : new Vector4(0.10f, 0.04f, 0.09f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, bgColour);
        using var box = ImRaii.Child($"##DRMatch_{roundIdx}_{matchIdx}", new Vector2(matchBoxW, matchBoxH), true, ImGuiWindowFlags.NoScrollbar);
        ImGui.PopStyleColor();
        if (!box.Success) return;
        DrawMatchContent(state, match, isCurrent, matchBoxW, matchBoxH, roundIdx, matchIdx);
    }

    private void DrawMatchContent(DeathrollTournamentState state, BracketMatch match, bool isCurrent, float boxW, float boxH, int roundIdx, int matchIdx)
    {
        var padX     = ImGui.GetStyle().WindowPadding.X;
        var padY     = ImGui.GetStyle().WindowPadding.Y;
        var innerW   = MathF.Max(10f, boxW - padX * 2f - 2f);
        var p1Label  = TruncateName(FormatSlot(match.Player1), innerW);
        var p2Label  = TruncateName(FormatSlot(match.Player2), innerW);
        var p1Colour = GetPlayerColour(match, isP1: true,  isCurrent, state);
        var p2Colour = GetPlayerColour(match, isP1: false, isCurrent, state);

        var lineH    = ImGui.GetTextLineHeight();
        var gapY     = ImGui.GetStyle().ItemSpacing.Y;
        var contentH = 3f * lineH + 2f * gapY;
        var startY   = padY + MathF.Max(0f, (boxH - padY * 2f - contentH) * 0.5f);
        ImGui.SetCursorPosY(startY);

        var p1Wins      = isCurrent && !match.IsResolved ? state.ActiveMatchPlayer1Wins : match.Player1Wins;
        var p2Wins      = isCurrent && !match.IsResolved ? state.ActiveMatchPlayer2Wins : match.Player2Wins;
        var p1Swappable = !string.IsNullOrEmpty(match.Player1);
        var p2Swappable = !string.IsNullOrEmpty(match.Player2);
        var p1Tellable  = p1Swappable && !DeathrollGameIds.IsBye(match.Player1);
        var p2Tellable  = p2Swappable && !DeathrollGameIds.IsBye(match.Player2);

        ImGui.SetCursorPosX(padX + MathF.Max(0f, (innerW - ImGui.CalcTextSize(p1Label).X) * 0.5f));
        ImGui.TextColored(p1Colour, p1Label);
        if (p1Swappable && ImGui.IsItemClicked()) ImGui.OpenPopup($"##DRSwapP1_{roundIdx}_{matchIdx}");
        if (p1Swappable && ImGui.IsItemHovered()) ImGui.SetTooltip("Click to swap player");
        if (p1Tellable) { ImGui.SameLine(0f, 4f); DrawTellBellButton(match.Player1); }
        if (p1Wins > 0 || p2Wins > 0) { ImGui.SameLine(); ImGui.TextDisabled($" {p1Wins}"); }
        DrawSwapPopup(state, match, roundIdx, matchIdx, true);

        var vsW = ImGui.CalcTextSize("vs").X;
        ImGui.SetCursorPosX(padX + MathF.Max(0f, (innerW - vsW) * 0.5f));
        ImGui.TextDisabled("vs");

        ImGui.SetCursorPosX(padX + MathF.Max(0f, (innerW - ImGui.CalcTextSize(p2Label).X) * 0.5f));
        ImGui.TextColored(p2Colour, p2Label);
        if (p2Swappable && ImGui.IsItemClicked()) ImGui.OpenPopup($"##DRSwapP2_{roundIdx}_{matchIdx}");
        if (p2Swappable && ImGui.IsItemHovered()) ImGui.SetTooltip("Click to swap player");
        if (p2Tellable) { ImGui.SameLine(0f, 4f); DrawTellBellButton(match.Player2); }
        if (p1Wins > 0 || p2Wins > 0) { ImGui.SameLine(); ImGui.TextDisabled($" {p2Wins}"); }
        DrawSwapPopup(state, match, roundIdx, matchIdx, false);
    }

    private void DrawTellBellButton(string playerNameWithWorld)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.85f, 0.1f, 1f));
        ImGui.TextUnformatted(FontAwesomeIcon.Bell.ToIconString());
        ImGui.PopStyleColor();
        ImGui.PopFont();

        var chat = this.config.DeathrollTournament.Chat;
        if (ImGui.IsItemClicked())
        {
            if (chat.UseCustomTurnReminderMessage)
            {
                var player = MessageFormat.DisplayPlayer(chat.TurnReminderMessage, playerNameWithWorld);
                this.chatQueue.Enqueue(chat.TurnReminderMessage.Replace("{player}", player));
            }
            else
            {
                MessageFormat.CopyTellToClipboard(playerNameWithWorld, MiniGamesEmporium.ChatGui);
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(chat.UseCustomTurnReminderMessage
                ? "Send turn reminder message"
                : "Copy /tell command to clipboard");
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
    }

    private static Vector4 GetPlayerColour(BracketMatch match, bool isP1, bool isCurrent, DeathrollTournamentState state)
    {
        var player = isP1 ? match.Player1 : match.Player2;
        if (string.IsNullOrEmpty(player)) return EmporiumNeonTheme.NeonCyan with { W = 0.35f };
        if (DeathrollGameIds.IsBye(player)) return new Vector4(0.4f, 0.4f, 0.4f, 0.6f);
        if (match.IsResolved && string.Equals(match.Winner, player, StringComparison.OrdinalIgnoreCase))
            return new Vector4(0.20f, 1f, 0.30f, 1f);
        if (match.IsResolved) return new Vector4(0.45f, 0.4f, 0.52f, 1f);
        if (isCurrent && !string.IsNullOrEmpty(state.CurrentTurnPlayerName))
        {
            var turnName = PlayerInfoService.StripWorld(state.CurrentTurnPlayerName);
            var pName    = PlayerInfoService.StripWorld(player);
            if (pName.Equals(turnName, StringComparison.OrdinalIgnoreCase))
                return new Vector4(1f, 0.92f, 0.15f, 1f);
        }
        return new Vector4(0.94f, 0.92f, 0.98f, 1f);
    }

    private static string FormatSlot(string slot)
    {
        if (string.IsNullOrEmpty(slot)) return "TBD";
        if (DeathrollGameIds.IsBye(slot)) return "BYE";
        return PlayerInfoService.StripWorld(slot);
    }

    private void DrawTrackerPane(DeathrollTournamentState state, float height)
    {
        using var pane = ImRaii.Child("##DRTrackerPane", new Vector2(-1f, height), false);
        if (!pane.Success) return;
        ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
        try
        {
            var match = this.deathrollService.GetCurrentMatch();
            if (match == null)
            {
                ImGui.TextDisabled("No active match.");
                return;
            }
            DrawMatchControls(state, match);
            ImGui.Spacing();
            if (state.ActiveMatchPhase == MatchPhase.DeterminingOrder || state.ActiveMatchPhase == MatchPhase.Deathrolling
                || state.ActiveMatchPhase == MatchPhase.GameOver || state.ActiveMatchPhase == MatchPhase.MatchComplete)
                DrawOrderRollSection(state, match);
            if (state.ActiveMatchPhase == MatchPhase.Deathrolling || state.ActiveMatchPhase == MatchPhase.GameOver
                || state.ActiveMatchPhase == MatchPhase.MatchComplete)
                DrawDeathrollSection(state, match);
        }
        finally
        {
            ImGui.PopTextWrapPos();
        }
    }

    private void DrawMatchControls(DeathrollTournamentState state, BracketMatch match)
    {
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Match Actions");
        ImGui.Spacing();
        if (state.ActiveMatchPhase == MatchPhase.NotStarted)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Play, "Start Round", "##DRStartRound"))
                this.deathrollService.StartCurrentMatch();
            ImGui.PopStyleColor(3);
        }
        else if (state.ActiveMatchPhase == MatchPhase.GameOver)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        PinkButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PinkButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  PinkButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Redo, "Next Game", "##DRNextGame"))
                this.deathrollService.StartNextGame();
            ImGui.PopStyleColor(3);
        }
        else if (state.ActiveMatchPhase == MatchPhase.MatchComplete)
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
            if (UIHelper.IconTextButton(FontAwesomeIcon.ArrowRight, "Next Match", "##DRNextMatch"))
            {
                this.deathrollService.MoveToNextMatch();
                this.deathrollService.StartCurrentMatch();
            }
            ImGui.PopStyleColor(3);
        }
        DrawManualWinnerButtons(state, match);
    }

    public void DrawMatchShouts()
    {
        var state = this.config.DeathrollTournamentSession;
        var match = this.deathrollService.GetCurrentMatch();
        if (state == null || match == null)
        {
            ImGui.TextDisabled("No active match.");
            return;
        }

        var chat = this.config.DeathrollTournament.Chat;
        var row  = new ShoutButtonRow();

        if (!chat.AutoAnnounceMatchup)
        {
            using (UIHelper.PushYellowButtonColours())
                if (row.Button(FontAwesomeIcon.Bullhorn, "Announce Matchup", "##DRShoutMatchup"))
                    AnnounceMatchup.Execute(match.Player1, match.Player2, this.config, this.chatQueue);
        }
        if (state.ActiveMatchPhase == MatchPhase.DeterminingOrder
            && state.LastOrderTiedValue > 0
            && !chat.AutoAnnounceRerollRandom10)
        {
            using (UIHelper.PushOrangeButtonColours())
                if (row.Button(FontAwesomeIcon.Dice, "Re-roll Random 10", "##DRShoutReroll"))
                    AnnounceRerollRandom10.Execute(state.LastOrderTiedValue, this.config, this.chatQueue);
        }
        if (state.ActiveMatchPhase == MatchPhase.Deathrolling
            && state.ActiveRollLog.Count == 0
            && !chat.AutoAnnounceFirstPlayer)
        {
            using (UIHelper.PushYellowButtonColours())
                if (row.Button(FontAwesomeIcon.Star, "First Player", "##DRShoutFirstPlayer"))
                    AnnounceFirstPlayer.Execute(state.CurrentTurnPlayerName, this.config, this.chatQueue);
        }
        if (!match.IsResolved
            && (state.ActiveMatchPlayer1Wins + state.ActiveMatchPlayer2Wins) > 0
            && state.ActiveMatchPhase is MatchPhase.GameOver or MatchPhase.DeterminingOrder
            && !chat.AutoAnnounceRoundWin)
        {
            var loserName   = PlayerInfoService.StripWorld(state.CurrentTurnPlayerName);
            var isLoserP1   = loserName.Equals(PlayerInfoService.StripWorld(match.Player1), StringComparison.OrdinalIgnoreCase);
            var roundWinner = isLoserP1 ? match.Player2 : match.Player1;
            var winnerWins  = isLoserP1 ? state.ActiveMatchPlayer2Wins : state.ActiveMatchPlayer1Wins;
            var loserWins   = isLoserP1 ? state.ActiveMatchPlayer1Wins : state.ActiveMatchPlayer2Wins;
            var roundIdx    = state.CurrentRoundIndex;
            var bestOf      = roundIdx < state.BestOfPerRound.Count ? state.BestOfPerRound[roundIdx] : 1;
            var gamesLeft   = bestOf - (state.ActiveMatchPlayer1Wins + state.ActiveMatchPlayer2Wins);
            using (UIHelper.PushOrangeButtonColours())
                if (row.Button(FontAwesomeIcon.Trophy, "Announce Round Win", "##DRShoutRoundWin"))
                    AnnounceRoundWin.Execute(roundWinner, winnerWins, loserWins, gamesLeft, this.config, this.chatQueue);
        }
        if (state.ActiveMatchPhase == MatchPhase.MatchComplete
            && !chat.AutoAnnounceMatchWin)
        {
            var winner     = match.Winner ?? string.Empty;
            var winnerIsP1 = PlayerInfoService.StripWorld(winner).Equals(PlayerInfoService.StripWorld(match.Player1), StringComparison.OrdinalIgnoreCase);
            var loser      = winnerIsP1 ? match.Player2 : match.Player1;
            var winnerWins = winnerIsP1 ? match.Player1Wins : match.Player2Wins;
            var loserWins  = winnerIsP1 ? match.Player2Wins : match.Player1Wins;
            using (new ImRaii.ColorDisposable()
                       .Push(ImGuiCol.Button,        PinkButton)
                       .Push(ImGuiCol.ButtonHovered, PinkButtonHovered)
                       .Push(ImGuiCol.ButtonActive,  PinkButtonActive))
                if (row.Button(FontAwesomeIcon.FlagCheckered, "Announce Match Win", "##DRShoutMatchWin"))
                    AnnounceMatchWin.Execute(winner, loser, winnerWins, loserWins, this.config, this.chatQueue);
        }
    }

    private void DrawManualWinnerButtons(DeathrollTournamentState state, BracketMatch match)
    {
        if (state.ActiveMatchPhase == MatchPhase.MatchComplete || match.IsResolved) return;
        var bestOf = state.CurrentRoundIndex < state.BestOfPerRound.Count ? state.BestOfPerRound[state.CurrentRoundIndex] : 1;
        ImGui.Spacing();
        var p1 = PlayerInfoService.StripWorld(match.Player1);
        var p2 = PlayerInfoService.StripWorld(match.Player2);
        if (bestOf >= 3)
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Check, FitIconButtonLabel(FontAwesomeIcon.Check, $"{p1} wins round"), "##DRManualP1RoundWin"))
                this.deathrollService.ManuallyAddRoundWin(match.Player1);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Check, FitIconButtonLabel(FontAwesomeIcon.Check, $"{p2} wins round"), "##DRManualP2RoundWin"))
                this.deathrollService.ManuallyAddRoundWin(match.Player2);
            ImGui.Spacing();
        }
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trophy, FitIconButtonLabel(FontAwesomeIcon.Trophy, $"{p1} wins match"), "##DRManualP1Win"))
            this.deathrollService.ManuallySetWinner(match.Player1);
        if (UIHelper.IconTextButton(FontAwesomeIcon.Trophy, FitIconButtonLabel(FontAwesomeIcon.Trophy, $"{p2} wins match"), "##DRManualP2Win"))
            this.deathrollService.ManuallySetWinner(match.Player2);
    }

    private static void DrawOrderRollSection(DeathrollTournamentState state, BracketMatch match)
    {
        ImGui.Separator();
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Order Roll  (/random 10)");
        ImGui.Spacing();
        var p1Roll = state.OrderRollPlayer1;
        var p2Roll = state.OrderRollPlayer2;
        var p1Colour = p1Roll > 0 ? EmporiumNeonTheme.SuccessMint : EmporiumNeonTheme.NeonCyan;
        var p2Colour = p2Roll > 0 ? EmporiumNeonTheme.SuccessMint : EmporiumNeonTheme.NeonMagenta;
        ImGui.TextColored(p1Colour, $"{PlayerInfoService.StripWorld(match.Player1)}: {(p1Roll > 0 ? p1Roll.ToString() : "waiting...")}");
        ImGui.TextColored(p2Colour, $"{PlayerInfoService.StripWorld(match.Player2)}: {(p2Roll > 0 ? p2Roll.ToString() : "waiting...")}");
        if (state.LastOrderTiedValue > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"Tie on {state.LastOrderTiedValue}! Both players must re-roll /random 10.");
        }
        else if (p1Roll > 0 && p2Roll > 0)
        {
            ImGui.Spacing();
            var goesFirst = p1Roll >= p2Roll ? PlayerInfoService.StripWorld(match.Player1) : PlayerInfoService.StripWorld(match.Player2);
            ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{goesFirst} goes first!");
        }
        ImGui.Spacing();
    }

    private static void DrawDeathrollSection(DeathrollTournamentState state, BracketMatch match)
    {
        ImGui.Separator();
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Deathroll Log");
        ImGui.Spacing();
        if (state.ActiveMatchPhase == MatchPhase.Deathrolling && !string.IsNullOrEmpty(state.CurrentTurnPlayerName))
        {
            var maxDisplay = state.CurrentDeathrollMax == 0 ? 1000 : state.CurrentDeathrollMax;
            ImGui.TextColored(EmporiumNeonTheme.WinGold, $"{PlayerInfoService.StripWorld(state.CurrentTurnPlayerName)} to roll /random {maxDisplay}");
        }
        ImGui.Spacing();
        var log = state.ActiveRollLog;
        if (log.Count == 0)
        {
            ImGui.TextDisabled("No rolls yet.");
            return;
        }
        var logH = MathF.Max(60f, ImGui.GetContentRegionAvail().Y - 8f);
        using var logBox = ImRaii.Child("##DRRollLog", new Vector2(-1f, logH), true);
        if (!logBox.Success) return;
        ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().X);
        for (var i = log.Count - 1; i >= 0; i--)
        {
            var entry  = log[i];
            var colour = entry.RollValue == 1 ? EmporiumNeonTheme.Bar777Red : new Vector4(0.94f, 0.92f, 0.98f, 1f);
            ImGui.TextColored(colour, $"{entry.PlayerName}  /random {entry.RollMax}  ->  {entry.RollValue}{(entry.RollValue == 1 ? "  DEAD!" : string.Empty)}");
        }
        ImGui.PopTextWrapPos();
    }

    private void DrawSwapPopup(DeathrollTournamentState state, BracketMatch match, int roundIdx, int matchIdx, bool isPlayer1)
    {
        var popupId = isPlayer1 ? $"##DRSwapP1_{roundIdx}_{matchIdx}" : $"##DRSwapP2_{roundIdx}_{matchIdx}";
        using var popup = ImRaii.Popup(popupId);
        if (!popup.Success) return;
        var currentSlot = isPlayer1 ? match.Player1 : match.Player2;
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, $"Swap: {FormatSlot(currentSlot)}");
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.IsWindowAppearing()) { this.swapFilter = string.Empty; ImGui.SetKeyboardFocusHere(); }
        ImGui.SetNextItemWidth(200f);
        ImGui.InputText("##DRSwapFilter", ref this.swapFilter, 64);
        ImGui.Spacing();

        var any            = false;
        var nearbyHeading  = false;
        foreach (var player in PlayerInfoService.GetNearbySorted())
        {
            if (!player.Contains(this.swapFilter, StringComparison.OrdinalIgnoreCase)) continue;
            if (IsAlreadyInBracket(player, state)) continue;
            if (!nearbyHeading) { ImGui.TextDisabled("Nearby"); nearbyHeading = true; }
            any = true;
            if (!ImGui.Selectable(player)) continue;
            ApplySwap(roundIdx, matchIdx, isPlayer1, player);
            return;
        }

        var elimHeading = false;
        var eliminated  = GetEliminatedPlayers(state);
        for (var i = 0; i < eliminated.Count; i++)
        {
            var (entry, roundLabel) = eliminated[i];
            if (!entry.Contains(this.swapFilter, StringComparison.OrdinalIgnoreCase)) continue;
            if (NameMatchesSlot(PlayerInfoService.StripWorld(entry), currentSlot)) continue;
            if (!elimHeading)
            {
                if (any) { ImGui.Spacing(); ImGui.Separator(); ImGui.Spacing(); }
                ImGui.TextDisabled("Eliminated");
                elimHeading = true;
            }
            any = true;
            bool picked;
            using (ImRaii.PushColor(ImGuiCol.Text, EmporiumNeonTheme.Bar777Red))
                picked = ImGui.Selectable($"{PlayerInfoService.StripWorld(entry)}  ({roundLabel})##DRElim{i}");
            if (!picked) continue;
            ApplySwap(roundIdx, matchIdx, isPlayer1, entry);
            return;
        }

        if (!any) ImGui.TextDisabled("No matches.");
    }

    private void ApplySwap(int roundIdx, int matchIdx, bool isPlayer1, string player)
    {
        this.deathrollService.SwapPlayerInBracket(roundIdx, matchIdx, isPlayer1, player);
        this.swapFilter = string.Empty;
        ImGui.CloseCurrentPopup();
    }

    private static List<(string Entry, string RoundLabel)> GetEliminatedPlayers(DeathrollTournamentState state)
    {
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(state.TournamentWinner))
            active.Add(PlayerInfoService.StripWorld(state.TournamentWinner));
        foreach (var round in state.Rounds)
            foreach (var match in round)
            {
                if (match.IsResolved && !string.IsNullOrEmpty(match.Winner)) continue;
                if (IsRealPlayer(match.Player1)) active.Add(PlayerInfoService.StripWorld(match.Player1));
                if (IsRealPlayer(match.Player2)) active.Add(PlayerInfoService.StripWorld(match.Player2));
            }

        var result = new List<(string, string)>();
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var r = 0; r < state.Rounds.Count; r++)
        {
            var roundLabel = r == state.Rounds.Count - 1 ? "Final" : $"R{r + 1}";
            foreach (var match in state.Rounds[r])
            {
                if (!match.IsResolved || string.IsNullOrEmpty(match.Winner)) continue;
                var winnerName = PlayerInfoService.StripWorld(match.Winner);
                var loser      = NameMatchesSlot(winnerName, match.Player1) ? match.Player2
                               : NameMatchesSlot(winnerName, match.Player2) ? match.Player1
                               : string.Empty;
                if (!IsRealPlayer(loser)) continue;
                var loserName = PlayerInfoService.StripWorld(loser);
                if (active.Contains(loserName) || !seen.Add(loserName)) continue;
                result.Add((loser, roundLabel));
            }
        }
        return result;
    }

    private static bool IsRealPlayer(string slot) =>
        !string.IsNullOrEmpty(slot) && !DeathrollGameIds.IsBye(slot);

    private static bool IsAlreadyInBracket(string nearbyEntry, DeathrollTournamentState state)
    {
        var nearbyName = PlayerInfoService.StripWorld(nearbyEntry);
        foreach (var round in state.Rounds)
            foreach (var match in round)
            {
                if (NameMatchesSlot(nearbyName, match.Player1)) return true;
                if (NameMatchesSlot(nearbyName, match.Player2)) return true;
            }
        return false;
    }

    private static bool NameMatchesSlot(string name, string slot)
    {
        if (string.IsNullOrEmpty(slot) || DeathrollGameIds.IsBye(slot)) return false;
        var slotName = PlayerInfoService.StripWorld(slot);
        return slotName.Equals(name, StringComparison.OrdinalIgnoreCase);
    }

    private void DrawTournamentComplete(DeathrollTournamentState state)
    {
        if (UIHelper.IconTextButton(FontAwesomeIcon.ChevronLeft, "Back to Bracket", "##DRBackToBracket"))
            this.viewingBracketAfterWin = true;

        ImGui.Spacing();

        var winnerName = PlayerInfoService.StripWorld(state.TournamentWinner ?? string.Empty);
        var isGilPrize = this.deathrollService.IsGilPrize();
        var pot        = this.deathrollService.ComputeTotalPot();

        this.card.Draw("##DRTournamentDoneCard", "Tournament Complete", CardAccent, CardTitle,
            () => DrawTournamentSummaryBody(isGilPrize, pot));

        this.card.Draw($"##DRWinnerCard_{winnerName}", winnerName, CardAccent, GoldColour,
            () => DrawWinnerPayoutBody(state, winnerName, isGilPrize, pot));
    }

    private void DrawTournamentSummaryBody(bool isGilPrize, long pot)
    {
        DrawTrophy();
        ImGui.Spacing();

        if (isGilPrize)
        {
            UIHelper.CentreTextScaled($"Total Pot: {pot:N0} Gil", GoldColour, 1.3f);
            return;
        }

        var prizeLabel = this.deathrollService.GetPrizeLabel();
        if (string.IsNullOrWhiteSpace(prizeLabel)) prizeLabel = "(not set)";
        UIHelper.CentreTextScaled($"Prize: {prizeLabel}", GoldColour, 1.3f);
    }

    private void DrawTrophy()
    {
        var tex = _trophyTexture?.GetWrapOrDefault();
        if (tex == null) return;
        var side = TrophySide * ImGuiHelpers.GlobalScale;
        UIHelper.CentreNext(side);
        ImGui.Image(tex.Handle, new Vector2(side, side));
    }

    private void DrawWinnerPayoutBody(DeathrollTournamentState state, string winnerName, bool isGilPrize, long pot)
    {
        var paid      = state.WinnerPayoutGil;
        var remaining = Math.Max(0L, pot - paid);

        UIHelper.CentreText("TOURNAMENT WINNER!", GoldColour);
        ImGui.Spacing();

        if (isGilPrize)
        {
            DrawPayoutFigures(pot, paid, remaining);
            ImGui.Spacing();
        }

        if (!this.config.DeathrollTournament.Chat.AutoAnnounceWinner)
        {
            using (UIHelper.PushButtonColours(YellowButton, YellowButtonHovered, YellowButtonActive))
            {
                if (UIHelper.CentredIconTextButton(FontAwesomeIcon.Bullhorn, "Announce Winner", "##DRAnnWinner"))
                    AnnounceTournamentWinner.Execute(state.TournamentWinner ?? string.Empty, pot, this.config, this.chatQueue);
            }
            ImGui.Spacing();
        }

        if (!isGilPrize)
        {
            UIHelper.CentreNextButtonRow((FontAwesomeIcon.Coins, "Trade Winner"));
            DrawTradeWinnerButton(winnerName);
            return;
        }

        var payoutRunning = this.autoPayoutService.IsRunning;
        var payoutIcon    = payoutRunning ? FontAwesomeIcon.Stop : FontAwesomeIcon.MoneyBillWave;
        var payoutLabel   = payoutRunning ? "Stop Auto Payout" : "Auto Payout";
        UIHelper.CentreNextButtonRow((FontAwesomeIcon.Coins, "Trade Winner"), (payoutIcon, payoutLabel));

        DrawTradeWinnerButton(winnerName);
        ImGui.SameLine();
        DrawWinnerAutoPayoutButton(winnerName, remaining);

        ImGui.Spacing();
        DrawPayoutProgressBar(pot, paid);
    }

    private void DrawTradeWinnerButton(string winnerName)
    {
        using var amber = UIHelper.PushAmberButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Coins, "Trade Winner", "##DRWinnerTrade"))
            SendTradeRequest.Execute(winnerName, this.chatQueue);
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

    private static void DrawPayoutProgressBar(long pot, long paid)
    {
        var progress = pot > 0 ? MathF.Min(1f, (float)paid / pot) : 1f;
        ImGui.ProgressBar(progress, new Vector2(-1f, ImGui.GetFrameHeight()), $"{progress * 100f:F0}% paid out");
    }

    private void DrawWinnerAutoPayoutButton(string winnerName, long remaining)
    {
        if (this.autoPayoutService.IsRunning)
        {
            using var red = UIHelper.PushRedButtonColours();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "Stop Auto Payout", "##DRStopAutoPayout"))
                this.autoPayoutService.Stop();
            return;
        }

        using var disabled = ImRaii.Disabled(remaining <= 0);
        using var green    = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.MoneyBillWave, "Auto Payout", "##DRAutoPayout"))
        {
            this.autoPayoutService.Start(
                winnerName,
                () =>
                {
                    var p = this.deathrollService.ComputeTotalPot();
                    var w = this.config.DeathrollTournamentSession?.WinnerPayoutGil ?? 0L;
                    return Math.Max(0L, p - w);
                },
                () => this.deathrollService.IsSessionActive());
        }
    }


    private static int ComputeRoundCount(int playerCount)
    {
        var size  = BracketMath.NextPowerOf2(playerCount);
        var count = 0;
        while (size > 1) { size >>= 1; count++; }
        return count;
    }

    private static string TruncateName(string text, float maxW)
    {
        if (ImGui.CalcTextSize(text).X <= maxW) return text;
        const string ellipsis = "...";
        var budget = maxW - ImGui.CalcTextSize(ellipsis).X;
        if (budget <= 0f) return ellipsis;
        var trimmed = text;
        while (trimmed.Length > 0 && ImGui.CalcTextSize(trimmed).X > budget)
            trimmed = trimmed[..^1];
        return trimmed + ellipsis;
    }

    private static string FitIconButtonLabel(FontAwesomeIcon icon, string label)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        if (UIHelper.CalcButtonSize(icon, label).X <= avail)
            return label;
        const string ellipsis = "...";
        var trimmed = label;
        while (trimmed.Length > 0 && UIHelper.CalcButtonSize(icon, trimmed + ellipsis).X > avail)
            trimmed = trimmed[..^1];
        return trimmed.Length == 0 ? ellipsis : trimmed + ellipsis;
    }

    private static (string CharName, string WorldName) GetCurrentTarget()
    {
        var playerChar = MiniGamesEmporium.TargetManager.Target as IPlayerCharacter;
        if (playerChar == null) return (string.Empty, string.Empty);
        return (playerChar.Name.TextValue, playerChar.HomeWorld.Value.Name.ToString());
    }
}
