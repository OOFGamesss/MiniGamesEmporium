using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Discord;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Deathroll Tournament Discord webhook configuration tab.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollDiscordWebhookTab
{
    private static readonly Vector4 CardAccent = EmporiumNeonTheme.DeathrollTournamentPink;
    private static readonly Vector4 CardTitle  = EmporiumNeonTheme.Secondary(CardAccent);

    private readonly ThemedCard card = new();

    private const int   UrlBufferLength      = 1536;
    private const int   UsernameBufferLength = 128;
    private const int   AliasBufferLength    = 64;
    private const float AliasInputWidth      = 150f;

    private static readonly Vector4 MutedText  = new(0.60f, 0.57f, 0.68f, 1f);
    private static readonly Vector4 ErrorText  = new(1f,    0.45f, 0.42f, 1f);
    private static readonly Vector4 ErrorFrame = new(0.48f, 0.06f, 0.06f, 1f);

    private readonly PluginConfiguration            _config;
    private readonly DeathrollWebhookService _webhookService;
    private readonly IPluginLog                     _log;
    private readonly ISharedImmediateTexture        _lobbyTexture;
    private readonly ISharedImmediateTexture        _bracketTexture;

    private List<string> _aliasDrafts = new();
    private List<string> _urlDrafts   = new();
    private int          _pendingRemoveIndex = -1;

    private string _usernameDraft          = string.Empty;
    private string _lastCommittedUsername  = string.Empty;
    private string _avatarUrlDraft         = string.Empty;
    private string _lastCommittedAvatarUrl = string.Empty;
    private bool   _appearanceInitialised  = false;

    public DeathrollDiscordWebhookTab(
        PluginConfiguration config,
        DeathrollWebhookService webhookService,
        IPluginLog log)
    {
        _config         = config;
        _webhookService = webhookService;
        _log            = log;

        var imagesDir = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.DirectoryName!,
            "Images", "Screenshots");
        _lobbyTexture   = MiniGamesEmporium.TextureProvider.GetFromFile(
            Path.Combine(imagesDir, "drt-example-lobby.png"));
        _bracketTexture = MiniGamesEmporium.TextureProvider.GetFromFile(
            Path.Combine(imagesDir, "drt-example-bracket.png"));
    }

    public void Draw()
    {
        SyncDraftsToEntries();
        EnsureAppearanceInitialised();

        this.card.Draw("##DRDiscSetupCard", "Webhook setup", CardAccent, CardTitle, DrawSetupGuide);
        this.card.Draw("##DRDiscUrlsCard", "Webhook URLs", CardAccent, CardTitle, DrawWebhookList);
        this.card.Draw("##DRDiscLookCard", "Webhook appearance (Delete original webhook to reset)", CardAccent, CardTitle, DrawAppearanceRows);
        this.card.Draw("##DRDiscPostsCard", "What this posts", CardAccent, CardTitle, DrawPostDescription);
        this.card.Draw("##DRDiscPreviewCard", "Discord Preview", CardAccent, CardTitle, DrawPreviewPair);
    }

    private List<DeathrollTournamentDiscordEntry> Entries => _config.DeathrollTournament.DiscordWebhooks;

    private void SyncDraftsToEntries()
    {
        if (_aliasDrafts.Count == Entries.Count) return;
        _aliasDrafts           = Entries.Select(e => e.Alias).ToList();
        _urlDrafts             = Entries.Select(e => e.Url).ToList();
        _appearanceInitialised = false;
    }

    private void EnsureAppearanceInitialised()
    {
        if (_appearanceInitialised) return;
        _usernameDraft          = _config.DeathrollTournament.WebhookUsername;
        _lastCommittedUsername  = _config.DeathrollTournament.WebhookUsername;
        _avatarUrlDraft         = _config.DeathrollTournament.WebhookAvatarUrl;
        _lastCommittedAvatarUrl = _config.DeathrollTournament.WebhookAvatarUrl;
        _appearanceInitialised  = true;
    }

    private void DrawWebhookList()
    {
        for (var i = 0; i < Entries.Count; i++)
            DrawWebhookRow(i);

        if (Entries.Count == 0)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, MutedText))
                ImGui.TextUnformatted("No webhooks configured. Add one below.");
        }

        if (_pendingRemoveIndex >= 0)
        {
            RemoveEntry(_pendingRemoveIndex);
            _pendingRemoveIndex = -1;
        }

        ImGuiHelpers.ScaledDummy(4f);
        if (UIHelper.IconTextButton(FontAwesomeIcon.Plus, "Add Webhook", "##DRAddWebhook"))
            AddEntry();
    }

    private void DrawWebhookRow(int i)
    {
        var entry = Entries[i];
        using var pushId = ImRaii.PushId(i);

        var enabled   = entry.Enabled;
        var urlBlank  = string.IsNullOrWhiteSpace(_urlDrafts[i]);
        using (ImRaii.PushColor(ImGuiCol.Text, ErrorText, entry.PostFailed))
        using (ImRaii.Disabled(urlBlank))
        {
            if (ImGui.Checkbox("##enabled", ref enabled))
                ToggleEnabled(i, enabled);
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(urlBlank
                ? "Enter a webhook URL before enabling."
                : entry.PostFailed
                    ? "Delivery failed: toggle Enable off then on to retry."
                    : "Enable to start posting.");

        ImGui.SameLine();

        var remaining = ImGui.GetContentRegionAvail().X;
        var deleteW   = UIHelper.CalcButtonSize(FontAwesomeIcon.Trash, string.Empty).X;
        var spacing   = ImGui.GetStyle().ItemSpacing.X;
        var aliasW    = AliasInputWidth * ImGuiHelpers.GlobalScale;
        var urlW      = MathF.Max(remaining - aliasW - deleteW - spacing * 2f, 40f);

        ImGui.SetNextItemWidth(aliasW);
        var alias = _aliasDrafts[i];
        ImGui.InputTextWithHint("##alias", "Discord Server Name", ref alias, AliasBufferLength);
        _aliasDrafts[i] = alias;
        if (ImGui.IsItemDeactivatedAfterEdit()) CommitAlias(i);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("A label to identify which Discord this webhook is for.");

        ImGui.SameLine();

        using (ImRaii.PushColor(ImGuiCol.FrameBg, ErrorFrame, entry.PostFailed))
        {
            ImGui.SetNextItemWidth(urlW);
            var url = _urlDrafts[i];
            ImGui.InputTextWithHint("##url", "Discord Webhook URL", ref url, UrlBufferLength);
            _urlDrafts[i] = url;
        }
        if (ImGui.IsItemDeactivatedAfterEdit()) CommitUrl(i);

        ImGui.SameLine();

        if (UIHelper.IconTextButton(FontAwesomeIcon.Trash, string.Empty, "##delete"))
            _pendingRemoveIndex = i;
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Remove this webhook.");

        ImGuiHelpers.ScaledDummy(2f);
    }

    private void DrawSetupGuide()
    {
        GuideBullet("In Discord, open channel settings (gear icon) for your tournament announcement channel.");
        GuideBullet("Go to Integrations → Webhooks. Create or select a webhook and copy its URL.");
        GuideBullet("Click Add Webhook below, give it an alias, paste the URL, and tick Enable.");
        GuideBullet("Toggle Enable off then on to force a retry or create a fresh embed after a failure.");
        ImGuiHelpers.ScaledDummy(8f);
    }

    private void DrawPostDescription()
    {
        Bullet("No session: posts a \"No Tournament Active\" embed with the Deathroll Tournament logo.");
        Bullet("Session open (registration): generates a player card showing all paid players with entry cost and pot.");
        Bullet("Tournament running: patches the embed with a generated bracket image and live match stats.");
        Bullet("Tournament complete: updates to show the winner and final pot total.");
        ImGuiHelpers.ScaledDummy(8f);
    }

    private void DrawPreviewPair()
    {
        var availW = ImGui.GetContentRegionAvail().X;
        const float gap = 8f;
        var imgW = (availW - gap) / 2f;

        using (ImRaii.Group())
            DrawPreviewImage(_lobbyTexture, imgW, "Registration");
        ImGui.SameLine(0f, gap);
        using (ImRaii.Group())
            DrawPreviewImage(_bracketTexture, imgW, "In Progress");

        ImGuiHelpers.ScaledDummy(8f);
    }

    private void DrawPreviewImage(ISharedImmediateTexture tex, float width, string caption)
    {
        var startX = ImGui.GetCursorPosX();
        if (tex.TryGetWrap(out var wrap, out _))
        {
            var imgH = width * ((float)wrap.Height / wrap.Width);
            ImGui.Image(wrap.Handle, new Vector2(width, imgH));
        }
        else
        {
            ImGui.Dummy(new Vector2(width, width * 0.75f));
        }
        var textW = ImGui.CalcTextSize(caption).X;
        ImGui.SetCursorPosX(startX + (width - textW) * 0.5f);
        using (ImRaii.PushColor(ImGuiCol.Text, MutedText))
            ImGui.TextUnformatted(caption);
    }

    private void GuideBullet(string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, MutedText))
        {
            ImGui.Bullet();
            ImGui.SameLine();
            ImGui.TextWrapped(text);
        }
        ImGuiHelpers.ScaledDummy(2f);
    }

    private static void Bullet(string text)
    {
        ImGui.Bullet();
        ImGui.SameLine();
        ImGui.TextWrapped(text);
        ImGuiHelpers.ScaledDummy(2f);
    }

    private void DrawAppearanceRows()
    {
        var availW = ImGui.GetContentRegionAvail().X;
        var labelW = ImGui.CalcTextSize("Image URL").X + ImGuiHelpers.GlobalScale * 8f;
        var inputW = availW - labelW - ImGui.GetStyle().ItemSpacing.X;

        using (ImRaii.PushColor(ImGuiCol.Text, MutedText))
            ImGui.TextUnformatted("Name");
        ImGui.SameLine(labelW);
        ImGui.SetNextItemWidth(inputW);
        ImGui.InputText("##DRWebhookName", ref _usernameDraft, UsernameBufferLength);
        if (ImGui.IsItemDeactivatedAfterEdit()) CommitUsername();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Display name shown on all Discord webhook messages.");

        ImGuiHelpers.ScaledDummy(4f);

        using (ImRaii.PushColor(ImGuiCol.Text, MutedText))
            ImGui.TextUnformatted("Image URL");
        ImGui.SameLine(labelW);
        ImGui.SetNextItemWidth(inputW);
        ImGui.InputText("##DRWebhookAvatar", ref _avatarUrlDraft, UrlBufferLength);
        if (ImGui.IsItemDeactivatedAfterEdit()) CommitAvatarUrl();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("URL of the image used as the webhook avatar and the idle embed image.");

        ImGuiHelpers.ScaledDummy(6f);
    }

    private void AddEntry()
    {
        Entries.Add(new DeathrollTournamentDiscordEntry());
        _aliasDrafts.Add(string.Empty);
        _urlDrafts.Add(string.Empty);
        _config.Save();
    }

    private void RemoveEntry(int i)
    {
        if (i < 0 || i >= Entries.Count) return;
        Entries.RemoveAt(i);
        _aliasDrafts.RemoveAt(i);
        _urlDrafts.RemoveAt(i);
        _config.Save();
        KickApply();
    }

    private void CommitAlias(int i)
    {
        if (i < 0 || i >= Entries.Count) return;
        var trimmed = _aliasDrafts[i].Trim();
        _aliasDrafts[i]  = trimmed;
        Entries[i].Alias = trimmed;
        _config.Save();
    }

    private void CommitUrl(int i)
    {
        if (i < 0 || i >= Entries.Count) return;
        var trimmed = _urlDrafts[i].Trim();
        _urlDrafts[i] = trimmed;
        var entry = Entries[i];
        if (trimmed == entry.Url) return;

        entry.MessageId  = null;
        entry.PostFailed = false;
        entry.Url        = trimmed;
        _config.Save();
        KickApply();
    }

    private void CommitUsername()
    {
        var trimmed = _usernameDraft.Trim();
        _usernameDraft = trimmed;
        if (trimmed == _lastCommittedUsername) return;

        _config.DeathrollTournament.WebhookUsername = trimmed;
        _lastCommittedUsername = trimmed;
        _config.Save();
        KickApply();
    }

    private void CommitAvatarUrl()
    {
        var trimmed = _avatarUrlDraft.Trim();
        _avatarUrlDraft = trimmed;
        if (trimmed == _lastCommittedAvatarUrl) return;

        _config.DeathrollTournament.WebhookAvatarUrl = trimmed;
        _lastCommittedAvatarUrl = trimmed;
        _config.Save();
        KickApply();
    }

    private void ToggleEnabled(int i, bool desired)
    {
        if (i < 0 || i >= Entries.Count) return;
        CommitUrl(i);
        Entries[i].Enabled = desired;
        _config.Save();
        KickApply();
    }

    private void KickApply()
    {
        _ = Task.Run(async () =>
        {
            try   { await _webhookService.ApplyEntryCommittedAsync(); }
            catch (Exception ex) { _log.Error(ex, "Deathroll Discord apply-entry failed from UI."); }
        });
    }
}
