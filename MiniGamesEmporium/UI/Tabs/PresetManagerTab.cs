using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Models;
using MiniGamesEmporium.Games.HigherLower.Config;
using MiniGamesEmporium.Games.Raffle.Config;
using MiniGamesEmporium.Games.VotingMadness.Config;
using MiniGamesEmporium.Models;
using MiniGamesEmporium.Services;
using MiniGamesEmporium.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>Draws the Preset Manager tab for selecting and managing presets.</summary>

namespace MiniGamesEmporium.UI.Tabs;
public sealed class PresetManagerTab
{
    private readonly PluginConfiguration config;
    private readonly PresetService presetService;
    private bool pendingAddPreset;
    private bool pendingRename;
    private bool pendingImport;
    private bool pendingExport;
    private string addNameBuffer = string.Empty;
    private string renameBuffer = string.Empty;
    private string importNameBuffer = string.Empty;
    private string importStringBuffer = string.Empty;
    private string? importParseError;
    private PresetExportPayload? parsedImportPayload;
    private string lastParsedString = string.Empty;
    private int fallbackPresetIndex = -1;
    private bool exportBar777 = true;
    private bool exportDeathroll = true;
    private bool exportRaffle = true;
    private bool exportHigherLower = true;
    private bool exportVotingMadness = true;
    private bool exportDiscord = false;
    private bool exportQueue = true;

    public PresetManagerTab(PluginConfiguration config, PresetService presetService)
    {
        this.config = config;
        this.presetService = presetService;
    }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Preset Manager");
        ImGui.TextDisabled("Save your current game rules, chat messages, and queue settings into named presets.");
        ImGui.TextDisabled("Selecting a preset from the dropdown applies it immediately.");
        ImGui.Separator();
        ImGui.Spacing();
        DrawImportExportRow();
        DrawSelectorRow();
        DrawPresetPreview();
        DrawAddPresetPopup();
        DrawRenamePresetPopup();
        DrawImportPresetPopup();
        DrawExportPresetPopup();
    }

    private void DrawSelectorRow()
    {
        DrawPresetDropdown();
        ImGui.SameLine(0, 6f);
        DrawManagementButtons();
    }

    private void DrawPresetDropdown()
    {
        var idx = this.config.ActivePresetIndex;
        var currentName = idx >= 0 && idx < this.config.Presets.Count
            ? this.config.Presets[idx].Name
            : string.Empty;
        ImGui.SetNextItemWidth(200f);
        using var combo = ImRaii.Combo("##PresetSelector", currentName);
        if (!combo.Success) return;
        for (var i = 0; i < this.config.Presets.Count; i++)
        {
            var isSelected = i == this.config.ActivePresetIndex;
            if (ImGui.Selectable(this.config.Presets[i].Name, isSelected))
                this.presetService.ApplyPreset(i);
            if (isSelected)
                ImGui.SetItemDefaultFocus();
        }
    }

    private void DrawImportExportRow()
    {
        DrawImportButton();
        ImGui.SameLine(0, 4f);
        DrawExportButton();
    }

    private void DrawManagementButtons()
    {
        DrawAddPresetButton();
        ImGui.SameLine(0, 4f);
        DrawRenameButton();
        if (this.config.Presets.Count <= 1) return;
        ImGui.SameLine(0, 4f);
        DrawDeleteButton();
    }

    private void DrawAddPresetButton()
    {
        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Add Preset", "##AddPreset"))
        {
            this.addNameBuffer = string.Empty;
            this.pendingAddPreset = true;
        }
    }

    private void DrawImportButton()
    {
        using var green = UIHelper.PushGreenButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.FileImport, "Import Preset", "##ImportPreset"))
        {
            this.importNameBuffer    = string.Empty;
            this.importStringBuffer  = string.Empty;
            this.importParseError    = null;
            this.parsedImportPayload = null;
            this.lastParsedString    = string.Empty;
            this.fallbackPresetIndex = this.presetService.GetDefaultFallbackIndex();
            this.pendingImport       = true;
        }
    }

    private void DrawRenameButton()
    {
        using var yellow = UIHelper.PushYellowButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Pen, "Rename", "##RenamePreset"))
        {
            this.renameBuffer = this.config.ActivePresetIndex >= 0 && this.config.ActivePresetIndex < this.config.Presets.Count
                ? this.config.Presets[this.config.ActivePresetIndex].Name
                : string.Empty;
            this.pendingRename = true;
        }
    }

    private void DrawExportButton()
    {
        using var red = UIHelper.PushRedButtonColours();
        if (UIHelper.IconTextButton(FontAwesomeIcon.FileExport, "Export to Clipboard", "##ExportPreset"))
            this.pendingExport = true;
    }

    private void DrawDeleteButton()
    {
        ImGui.PushStyleColor(ImGuiCol.Button,        EmporiumNeonTheme.Bar777Red);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0.22f, 0.38f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(0.72f, 0.08f, 0.22f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text,          new Vector4(1f, 0.98f, 0.98f, 1f));
        try
        {
            if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, "Delete Preset", "##DeletePreset"))
                this.presetService.DeletePreset(this.config.ActivePresetIndex);
        }
        finally
        {
            ImGui.PopStyleColor(4);
        }
    }

    private void DrawAddPresetPopup()
    {
        if (this.pendingAddPreset)
        {
            ImGui.OpenPopup("##AddPreset");
            this.pendingAddPreset = false;
        }
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.PopupModal("##AddPreset", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Add Preset");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Enter a name for the new preset:");
        ImGui.SetNextItemWidth(260f);
        ImGui.InputText("##AddPresetName", ref this.addNameBuffer, 64);
        var addError = GetAddNameError(this.addNameBuffer.Trim());
        DrawNameError(addError);
        ImGui.Spacing();
        DrawAddConfirmButtons(addError);
    }

    private void DrawAddConfirmButtons(string? error)
    {
        {
            using var disabled = ImRaii.Disabled(error != null);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Create", "##AddPresetConfirm"))
            {
                this.presetService.CreatePreset(this.addNameBuffer.Trim());
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.SameLine(0, 6f);
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##AddPresetCancel"))
            ImGui.CloseCurrentPopup();
    }

    private string? GetAddNameError(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name cannot be empty.";
        if (this.config.Presets.Exists(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            return "A preset with that name already exists.";
        return null;
    }

    private void DrawRenamePresetPopup()
    {
        if (this.pendingRename)
        {
            ImGui.OpenPopup("##RenamePreset");
            this.pendingRename = false;
        }
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.PopupModal("##RenamePreset", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Rename Preset");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Enter a new name:");
        ImGui.SetNextItemWidth(260f);
        ImGui.InputText("##RenamePresetName", ref this.renameBuffer, 64);
        var renameError = GetRenameNameError(this.renameBuffer.Trim());
        DrawNameError(renameError);
        ImGui.Spacing();
        DrawRenameConfirmButtons(renameError);
    }

    private void DrawRenameConfirmButtons(string? error)
    {
        {
            using var disabled = ImRaii.Disabled(error != null);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Check, "Rename", "##RenameConfirm"))
            {
                this.presetService.RenamePreset(this.config.ActivePresetIndex, this.renameBuffer.Trim());
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.SameLine(0, 6f);
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##RenameCancel"))
            ImGui.CloseCurrentPopup();
    }

    private string? GetRenameNameError(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name cannot be empty.";
        for (var i = 0; i < this.config.Presets.Count; i++)
        {
            if (i == this.config.ActivePresetIndex) continue;
            if (string.Equals(this.config.Presets[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return "A preset with that name already exists.";
        }
        return null;
    }

    private void DrawImportPresetPopup()
    {
        if (this.pendingImport)
        {
            ImGui.OpenPopup("##ImportPreset");
            this.pendingImport = false;
        }
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(420f, 0f), ImGuiCond.Always);
        using var popup = ImRaii.PopupModal("##ImportPreset", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Import Preset");
        ImGui.Separator();
        ImGui.Spacing();
        DrawImportNameField();
        ImGui.Spacing();
        DrawImportStringField();
        ImGui.Spacing();
        DrawImportConfirmButtons();
    }

    private void DrawImportNameField()
    {
        ImGui.TextUnformatted("Preset Name:");
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputText("##ImportPresetName", ref this.importNameBuffer, 64);
        DrawNameError(GetAddNameError(this.importNameBuffer.Trim()));
    }

    private void DrawImportStringField()
    {
        ImGui.TextUnformatted("Preset String:");
        ImGui.InputTextMultiline("##ImportPresetString", ref this.importStringBuffer, 4096, new Vector2(-1f, 80f));
        UpdateParsedPayload();
        DrawSectionSummary();
        if (this.importParseError != null)
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, this.importParseError);
        else
            ImGui.Dummy(new Vector2(0f, ImGui.GetTextLineHeight()));
    }

    private void UpdateParsedPayload()
    {
        if (this.importStringBuffer == this.lastParsedString) return;
        this.lastParsedString    = this.importStringBuffer;
        this.importParseError    = null;
        this.parsedImportPayload = !string.IsNullOrWhiteSpace(this.importStringBuffer)
            && this.presetService.TryParseExportString(this.importStringBuffer, out var p) ? p : null;
    }

    private void DrawSectionSummary()
    {
        if (this.parsedImportPayload == null) return;
        var payload  = this.parsedImportPayload;
        var detected = BuildSectionItems(payload.Bar777 != null, payload.DeathrollTournament != null, payload.Raffle != null, payload.HigherLower != null, payload.VotingMadness != null, payload.Discord != null, payload.QueueKeyword != null);
        var missing  = BuildSectionItems(payload.Bar777 == null, payload.DeathrollTournament == null, payload.Raffle == null, payload.HigherLower == null, payload.VotingMadness == null, payload.Discord == null, payload.QueueKeyword == null);
        if (detected.Count > 0)
        {
            ImGui.TextColored(EmporiumNeonTheme.SuccessMint, "Detected:");
            foreach (var name in detected)
                ImGui.TextColored(EmporiumNeonTheme.SuccessMint, $"  • {name}");
        }
        if (missing.Count > 0)
        {
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Missing:");
            foreach (var name in missing)
                ImGui.TextColored(EmporiumNeonTheme.WarnAmber, $"  • {name}");
            DrawFallbackSelector();
        }
    }

    private void DrawFallbackSelector()
    {
        ImGui.Spacing();
        ImGui.TextUnformatted("Fill missing sections from:");
        ImGui.SameLine(0, 6f);
        var previewName = this.fallbackPresetIndex >= 0 && this.fallbackPresetIndex < this.config.Presets.Count
            ? this.config.Presets[this.fallbackPresetIndex].Name
            : "Plugin Defaults";
        ImGui.SetNextItemWidth(180f);
        using var combo = ImRaii.Combo("##FallbackPreset", previewName);
        if (!combo.Success) return;
        if (ImGui.Selectable("Plugin Defaults", this.fallbackPresetIndex < 0))
            this.fallbackPresetIndex = -1;
        for (var i = 0; i < this.config.Presets.Count; i++)
        {
            if (ImGui.Selectable(this.config.Presets[i].Name, i == this.fallbackPresetIndex))
                this.fallbackPresetIndex = i;
        }
    }

    private static List<string> BuildSectionItems(bool bar777, bool deathroll, bool raffle, bool higherlower, bool votingMadness, bool discord, bool queue)
    {
        var parts = new List<string>(7);
        if (bar777)        parts.Add("BAR 777");
        if (deathroll)     parts.Add("Deathroll Tournament");
        if (raffle)        parts.Add("Raffle");
        if (higherlower)   parts.Add("Higher/Lower");
        if (votingMadness) parts.Add("Voting Madness");
        if (discord)       parts.Add("Discord Webhook");
        if (queue)         parts.Add("Queue Actions");
        return parts;
    }

    private void DrawImportConfirmButtons()
    {
        var nameError = GetAddNameError(this.importNameBuffer.Trim());
        var canImport = nameError == null && !string.IsNullOrWhiteSpace(this.importStringBuffer);
        {
            using var disabled = ImRaii.Disabled(!canImport);
            if (UIHelper.IconTextButton(FontAwesomeIcon.FileImport, "Import", "##ImportConfirm"))
            {
                int? fallback = this.fallbackPresetIndex >= 0 ? this.fallbackPresetIndex : null;
                if (this.presetService.TryImportPreset(this.importNameBuffer.Trim(), this.importStringBuffer.Trim(), fallback, out var parseError))
                    ImGui.CloseCurrentPopup();
                else
                    this.importParseError = parseError;
            }
        }
        ImGui.SameLine(0, 6f);
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##ImportCancel"))
        {
            this.importParseError = null;
            ImGui.CloseCurrentPopup();
        }
    }

    private void DrawExportPresetPopup()
    {
        if (this.pendingExport)
        {
            ImGui.OpenPopup("##ExportPreset");
            this.pendingExport = false;
        }
        var centre = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(centre, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        using var popup = ImRaii.PopupModal("##ExportPreset", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
        if (!popup.Success) return;
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Export Preset to Clipboard");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Select which settings to include:");
        ImGui.Spacing();
        DrawExportCheckboxes();
        ImGui.Spacing();
        DrawExportConfirmButtons();
    }

    private void DrawExportCheckboxes()
    {
        ImGui.Checkbox("BAR 777",              ref this.exportBar777);
        ImGui.Checkbox("Deathroll Tournament", ref this.exportDeathroll);
        ImGui.Checkbox("Raffle",               ref this.exportRaffle);
        ImGui.Checkbox("Higher/Lower",         ref this.exportHigherLower);
        ImGui.Checkbox("Voting Madness",       ref this.exportVotingMadness);
        ImGui.Checkbox("Discord Webhook",      ref this.exportDiscord);
        if (this.exportDiscord)
        {
            ImGui.Indent(16f);
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Warning: The webhook URL grants anyone who has it the");
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "ability to send messages through your webhook.");
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Only share with trusted recipients.");
            ImGui.Unindent(16f);
        }
        ImGui.Checkbox("Queue Actions", ref this.exportQueue);
    }

    private void DrawExportConfirmButtons()
    {
        var noneSelected = !this.exportBar777 && !this.exportDeathroll && !this.exportRaffle && !this.exportHigherLower && !this.exportVotingMadness && !this.exportDiscord && !this.exportQueue;
        {
            using var disabled = ImRaii.Disabled(noneSelected);
            if (UIHelper.IconTextButton(FontAwesomeIcon.Clipboard, "Copy to Clipboard", "##ExportCopy"))
            {
                var str = this.presetService.BuildExportString(
                    this.exportBar777, this.exportDeathroll, this.exportRaffle, this.exportHigherLower, this.exportVotingMadness, this.exportDiscord, this.exportQueue);
                ImGui.SetClipboardText(str);
                ImGui.CloseCurrentPopup();
            }
        }
        ImGui.SameLine(0, 6f);
        if (UIHelper.IconTextButton(FontAwesomeIcon.Times, "Cancel", "##ExportCancel"))
            ImGui.CloseCurrentPopup();
    }

    private static void DrawNameError(string? error)
    {
        if (error != null)
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, error);
        else
            ImGui.Dummy(new Vector2(0f, ImGui.GetTextLineHeight()));
    }

    private void DrawPresetPreview()
    {
        ImGui.Spacing();
        ImGui.TextDisabled("Current settings:");
        ImGui.Separator();
        ImGui.Spacing();
        using var child = ImRaii.Child("##PresetPreview", new Vector2(0f, 0f), true);
        if (!child.Success) return;
        DrawBar777Section(this.config.Bar777);
        ImGui.Spacing();
        DrawDeathrollSection(this.config.DeathrollTournament);
        ImGui.Spacing();
        DrawRaffleSection(this.config.Raffle);
        ImGui.Spacing();
        DrawHigherLowerSection(this.config.HigherLower);
        ImGui.Spacing();
        DrawVotingMadnessSection(this.config.VotingMadness);
        ImGui.Spacing();
        DrawQueueSection(this.config.QueueKeyword, this.config.QueueJoinChannels);
    }

    private static void DrawBar777Section(Bar777Config b)
    {
        ImGui.TextColored(EmporiumNeonTheme.Bar777Red, "BAR 777");
        ImGui.Separator();
        ImGui.Spacing();
        DrawBar777GameRows(b);
        ImGui.Spacing();
        ImGui.TextDisabled("Chat Messages");
        DrawBar777ChatRows(b.Chat);
    }

    private static void DrawBar777GameRows(Bar777Config b)
    {
        using var t = ImRaii.Table("##Bar777Game", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawRow("Display Name",    b.CustomName);
        DrawRow("Cost per Roll",   b.CostPerRoll.ToString("N0") + " gil");
        DrawRow("Max Rolls",       b.MaxRolls.ToString());
        DrawRow("Win Number",      b.WinNumber.ToString());
        DrawRow("Boosted Pot",     b.BoostedPot.ToString("N0") + " gil");
        DrawRow("Trades to Pot",   b.TradesToPotPercent + "%");
        DrawBoolRow("Use Queue",       b.UseQueue);
        DrawBoolRow("Auto Catch Roll", b.AutoCatchRoll);
    }

    private static void DrawBar777ChatRows(Bar777ChatConfig c)
    {
        using var t = ImRaii.Table("##Bar777Chat", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawRow("Chat Channel",           c.Channel.ToString());
        DrawBoolRow("Auto Send Halfway",      c.AutoSendHalfway);
        DrawBoolRow("Auto Send Unlucky",      c.AutoSendUnlucky);
        DrawBoolRow("Auto Send Win Shout",    c.AutoSendWinShout);
        DrawBoolRow("Auto Start Rolls",       c.AutoStartRolls);
        DrawBoolRow("Auto Send Join Queue",   c.AutoSendJoinQueue);
        DrawBoolRow("Auto Send Reminder",     c.AutoSendReminderToPlay);
        DrawRow("Reminder Threshold",   c.ReminderQueueThreshold.ToString());
        DrawRow("Rules Message",        c.RulesMessage);
        DrawRow("Start Rolls",          c.StartRollsMessage);
        DrawRow("Request Gil",          c.TellAmountRequestMessage);
        DrawRow("Request Gil (Buyer)",  c.TellBuyerRequestMessage);
        DrawRow("Halfway Message",      c.HalfwayMessage);
        DrawRow("Unlucky Message",      c.UnluckyMessage);
        DrawRow("Win Shout",            c.WinShoutMessage);
        DrawRow("Yell Pot Message",     c.YellPotMessage);
        DrawRow("Join Queue Message",   c.JoinQueueMessage);
        DrawRow("Reminder to Play",     c.ReminderToPlayMessage);
        DrawRow("Announce Keyword",     c.AnnounceKeywordMessage);
        DrawRow("Next Player Up",       c.NextPlayerUpMessage);
    }

    private static void DrawDeathrollSection(DeathrollTournamentConfig d)
    {
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Deathroll Tournament");
        ImGui.Separator();
        ImGui.Spacing();
        DrawDeathrollGameRows(d);
        ImGui.Spacing();
        ImGui.TextDisabled("Chat Messages");
        DrawDeathrollChatRows(d.Chat);
        ImGui.Spacing();
        ImGui.TextDisabled("Discord");
        DrawDeathrollDiscordRows(d);
    }

    private static void DrawDeathrollGameRows(DeathrollTournamentConfig d)
    {
        using var t = ImRaii.Table("##DeathrollGame", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawRow("Entry Cost",           d.EntryCost.ToString("N0") + " gil");
        DrawRow("Boosted Pot",          d.BoostedPot.ToString("N0") + " gil");
        DrawRow("Prize Type",           d.PrizeType == DeathrollPrizeType.Gil ? "Gil Pot" : d.PrizeType.ToString());
        switch (d.PrizeType)
        {
            case DeathrollPrizeType.Item:
                DrawRow("Prize Item", string.IsNullOrWhiteSpace(d.PrizeItemName) ? "(not set)" : d.PrizeItemName);
                break;
            case DeathrollPrizeType.Custom:
                DrawRow("Prize Text", string.IsNullOrWhiteSpace(d.PrizeCustomText) ? "(not set)" : d.PrizeCustomText);
                break;
        }
        DrawRow("Best-Of Per Round",    string.Join(", ", d.BestOfPerRound));
        DrawBoolRow("Auto Next Match",  d.AutoNextMatch);
        DrawRow("Auto Next Delay",      d.AutoNextMatchDelaySeconds + "s");
        DrawBoolRow("Auto Catch Round", d.AutoCatchNextRound);
        DrawBoolRow("Auto Join Keyword", d.AutoJoinKeyword);
        DrawRow("Join Keyword",         d.JoinKeyword);
        DrawRow("Join Channels",        SummariseChannels(d.JoinChannels));
        DrawBoolRow("Betting Enabled",  d.BettingEnabled);
        if (d.BettingEnabled)
        {
            DrawRow("Bet Unit",             d.BetUnit.ToString("N0") + " gil");
            DrawBoolRow("Auto Bet Keyword", d.AutoBetKeyword);
            DrawRow("Bet Keyword",          d.BetKeyword);
            DrawRow("Bet Channels",         SummariseChannels(d.BetChannels));
        }
        DrawRow("Web Venue Name",  string.IsNullOrWhiteSpace(d.WebVenueName) ? "(not set)" : d.WebVenueName);
        DrawRow("Web Venue Image", string.IsNullOrWhiteSpace(d.WebVenueImageUrl) ? "(not set)" : d.WebVenueImageUrl);
    }

    private static string SummariseChannels(QueueConfig c)
    {
        var parts = new List<string>();
        if (c.Say) parts.Add("Say");
        if (c.Shout) parts.Add("Shout");
        if (c.Yell) parts.Add("Yell");
        if (c.TellIncoming) parts.Add("Tell");
        return parts.Count > 0 ? string.Join(", ", parts) : "(none)";
    }

    private static void DrawDeathrollChatRows(DeathrollTournamentChatConfig c)
    {
        using var t = ImRaii.Table("##DeathrollChat", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawBoolRow("Auto Announce Matchup",   c.AutoAnnounceMatchup);
        DrawBoolRow("Auto Announce Prize",     c.AutoAnnouncePrize);
        DrawBoolRow("Auto Announce Winner",    c.AutoAnnounceWinner);
        DrawBoolRow("Auto Reroll Rand 10",     c.AutoAnnounceRerollRandom10);
        DrawBoolRow("Auto First Player",       c.AutoAnnounceFirstPlayer);
        DrawBoolRow("Auto Round Win",          c.AutoAnnounceRoundWin);
        DrawBoolRow("Auto Match Win",          c.AutoAnnounceMatchWin);
        DrawBoolRow("Auto Betting Open",       c.AutoAnnounceBettingOpen);
        DrawBoolRow("Auto Bet Winners",        c.AutoAnnounceBetWinners);
        DrawBoolRow("Custom Turn Reminder",    c.UseCustomTurnReminderMessage);
        DrawRow("Announce Bracket",       c.AnnounceBracketMessage);
        DrawRow("Announce Matchup",       c.AnnounceMatchupMessage);
        DrawRow("Announce Prize",         c.AnnouncePrizeMessage);
        DrawRow("Tournament Winner",      c.AnnounceTournamentWinnerMessage);
        DrawRow("Turn Reminder",          c.TurnReminderMessage);
        DrawRow("Request Gil",            c.RequestGilMessage);
        DrawRow("Request Gil (Buyer)",    c.RequestGilBuyerMessage);
        DrawRow("Reroll Random 10",       c.RerollRandom10Message);
        DrawRow("First Player",           c.AnnounceFirstPlayerMessage);
        DrawRow("Round Win",              c.AnnounceRoundWinMessage);
        DrawRow("Match Win",              c.AnnounceMatchWinMessage);
        DrawRow("Betting Open",           c.AnnounceBettingOpenMessage);
        DrawRow("Betting Pot",            c.AnnounceBettingPotMessage);
        DrawRow("Request Bet Gil",        c.RequestBetGilMessage);
        DrawRow("Bet Winner",             c.AnnounceBetWinnerMessage);
        DrawRow("Bet Winners",            c.AnnounceBetWinnersMessage);
        DrawRow("Announce Web Link",      c.AnnounceWebviewMessage);
    }

    private static void DrawDeathrollDiscordRows(DeathrollTournamentConfig d)
    {
        using var appearance = ImRaii.Table("##DeathrollDiscordAppearance", 2, ImGuiTableFlags.RowBg);
        if (appearance.Success)
        {
            SetupPreviewColumns();
            DrawRow("Display Name", d.WebhookUsername);
            DrawRow("Avatar URL",   d.WebhookAvatarUrl);
        }
        appearance.Dispose();

        if (d.DiscordWebhooks.Count == 0)
        {
            ImGui.TextDisabled("  (no webhooks)");
            return;
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Webhooks");
        using var webhooks = ImRaii.Table("##DeathrollDiscordWebhooks", 2, ImGuiTableFlags.RowBg);
        if (!webhooks.Success) return;
        SetupPreviewColumns();
        foreach (var entry in d.DiscordWebhooks)
        {
            var alias = string.IsNullOrWhiteSpace(entry.Alias) ? "(no alias)" : entry.Alias;
            DrawBoolRow(alias, entry.Enabled);
        }
    }

    private static void DrawHigherLowerSection(HigherLowerConfig h)
    {
        ImGui.TextColored(EmporiumNeonTheme.HigherLowerOrange, "Higher/Lower");
        ImGui.Separator();
        ImGui.Spacing();
        DrawHigherLowerGameRows(h);
        ImGui.Spacing();
        ImGui.TextDisabled("Chat Messages");
        DrawHigherLowerChatRows(h.Chat);
    }

    private static void DrawVotingMadnessSection(VotingMadnessConfig v)
    {
        ImGui.TextColored(EmporiumNeonTheme.VotingMadnessLime, "Voting Madness");
        ImGui.Separator();
        ImGui.Spacing();
        DrawVotingMadnessGameRows(v);
        ImGui.Spacing();
        ImGui.TextDisabled("Chat Messages");
        DrawVotingMadnessChatRows(v.Chat);
    }

    private static void DrawVotingMadnessGameRows(VotingMadnessConfig v)
    {
        using var t = ImRaii.Table("##VotingMadnessGame", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawRow("Options", string.Join(", ", v.Options.Where(o => !string.IsNullOrWhiteSpace(o))));
        DrawRow("Vote Channels", SummariseChannels(v.VoteChannels));
        DrawBoolRow("Multiple Choice", v.MultipleChoice);
        DrawBoolRow("Allow Multiple Votes", v.AllowMultipleVotes);
        DrawRow("Close Time", v.CloseHour < 0 ? "(none)" : $"{v.CloseHour:00}:{v.CloseMinute:00}");
    }

    private static void DrawVotingMadnessChatRows(VotingMadnessChatConfig c)
    {
        using var t = ImRaii.Table("##VotingMadnessChat", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawRow("Announce Options", c.AnnounceOptionsMessage);
        DrawRow("Vote Started", c.VoteStartedMessage);
        DrawRow("Closing Time", c.AnnounceClosingTimeMessage);
        DrawRow("Standings", c.AnnounceStandingsMessage);
        DrawRow("Vote Ended", c.VoteEndedMessage);
        DrawRow("Winning Vote", c.AnnounceWinningVoteMessage);
        DrawRow("Tie", c.AnnounceTieMessage);
    }

    private static void DrawHigherLowerGameRows(HigherLowerConfig h)
    {
        using var t = ImRaii.Table("##HigherLowerGame", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawRow("Entry Cost",              h.EntryCost.ToString("N0") + " gil");
        DrawRow("Boosted Pot",             h.BoostedPot.ToString("N0") + " gil");
        DrawRow("Dice Sides",              h.DiceSides.ToString());
        DrawRow("Target Rounds",           h.TargetRounds.ToString());
        DrawBoolRow("Auto Win Count",      h.AutoWinCount);
        DrawBoolRow("Allow Multiple Winners", h.AllowMultipleWinners);
        DrawRow("Trades to Pot",           h.TradesToPotPercent + "%");
    }

    private static void DrawHigherLowerChatRows(HigherLowerChatConfig c)
    {
        using var t = ImRaii.Table("##HigherLowerChat", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawBoolRow("Auto Send Loss",      c.AutoSendLoss);
        DrawBoolRow("Auto Send Let's Play", c.AutoSendLetsPlay);
        DrawBoolRow("Auto Send Ask Guess", c.AutoSendAskGuess);
        DrawRow("Rules Message",       c.RulesMessage);
        DrawRow("Request Gil",         c.TellAmountRequestMessage);
        DrawRow("Win Shout",           c.WinShoutMessage);
        DrawRow("Loss (Unlucky)",      c.LossUnluckyMessage);
        DrawRow("Loss (Winning)",      c.LossWinningMessage);
        DrawRow("Let's Play",          c.LetsPlayMessage);
        DrawRow("Ask Guess",           c.AskGuessMessage);
        DrawRow("Announce Pot",        c.AnnouncePotMessage);
    }

    private static void DrawRaffleSection(RaffleConfig r)
    {
        ImGui.TextColored(EmporiumNeonTheme.RaffleTeal, "Raffle");
        ImGui.Separator();
        ImGui.Spacing();
        DrawRaffleGameRows(r);
        ImGui.Spacing();
        ImGui.TextDisabled("Chat Messages");
        DrawRaffleChatRows(r.Chat);
    }

    private static void DrawRaffleGameRows(RaffleConfig r)
    {
        using var t = ImRaii.Table("##RaffleGame", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawRow("Ticket Cost",          r.TicketCost.ToString("N0") + " gil");
        DrawRow("Max Tickets/Player",   r.MaxTicketsPerPlayer.ToString());
        DrawRow("Boosted Pot",          r.BoostedPot.ToString("N0") + " gil");
        DrawRow("Trades to Pot",        r.TradesToPotPercent + "%");
        DrawRow("Close Time",           r.CloseHour < 0 ? "(none)" : $"{r.CloseHour:00}:{r.CloseMinute:00}");
        DrawBoolRow("Auto Join Keyword", r.AutoJoinKeyword);
        DrawRow("Join Keyword",         r.JoinKeyword);
        DrawRow("Join Channels",        SummariseChannels(r.JoinChannels));
    }

    private static void DrawRaffleChatRows(RaffleChatConfig c)
    {
        using var t = ImRaii.Table("##RaffleChat", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawBoolRow("Auto Announce Winner",  c.AutoAnnounceWinner);
        DrawBoolRow("Auto Send Ticket Numbers", c.AutoSendTicketNumbers);
        DrawRow("Announce Pot",         c.AnnouncePotMessage);
        DrawRow("Tickets Sold",         c.AnnounceTicketsSoldMessage);
        DrawRow("Closing Time",         c.AnnounceClosingTimeMessage);
        DrawRow("Join Reminder",        c.AnnounceJoinReminderMessage);
        DrawRow("Raffle Closed",        c.AnnounceRaffleClosedMessage);
        DrawRow("Announce Winner",      c.AnnounceWinnerMessage);
        DrawRow("Request Gil",          c.RequestGilMessage);
        DrawRow("Request Gil (Buyer)",  c.RequestGilBuyerMessage);
        DrawRow("Ticket Numbers",       c.TicketNumbersMessage);
    }

    private static void DrawQueueSection(string keyword, QueueConfig channels)
    {
        ImGui.TextColored(EmporiumNeonTheme.NeonCyan, "Queue Settings");
        ImGui.Separator();
        ImGui.Spacing();
        using var t = ImRaii.Table("##QueuePreview", 2, ImGuiTableFlags.RowBg);
        if (!t.Success) return;
        SetupPreviewColumns();
        DrawRow("Join Keyword",        keyword);
        DrawBoolRow("Say",             channels.Say);
        DrawBoolRow("Shout",           channels.Shout);
        DrawBoolRow("Yell",            channels.Yell);
        DrawBoolRow("Tell (Incoming)", channels.TellIncoming);
    }

    private static void SetupPreviewColumns()
    {
        ImGui.TableSetupColumn("Setting", ImGuiTableColumnFlags.WidthFixed, 160f);
        ImGui.TableSetupColumn("Value",   ImGuiTableColumnFlags.WidthStretch);
    }

    private static void DrawRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(1);
        ImGui.TextUnformatted(value);
    }

    private static void DrawBoolRow(string label, bool value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextDisabled(label);
        ImGui.TableSetColumnIndex(1);
        if (value)
            ImGui.TextColored(EmporiumNeonTheme.SuccessMint, "Enabled");
        else
            ImGui.TextDisabled("Off");
    }
}
