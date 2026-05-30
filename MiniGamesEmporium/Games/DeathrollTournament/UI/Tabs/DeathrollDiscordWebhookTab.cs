using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Services;
using MiniGamesEmporium.UI.Components;

/// <summary>Draws the Deathroll Tournament Discord webhook configuration tab: URL input, delivery status warning, setup guide, post behaviour reference, and Discord embed previews.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollDiscordWebhookTab
{
    private const int UrlBufferLength      = 1536;
    private const int UsernameBufferLength = 128;

    private static readonly Vector4 MutedText  = new(0.60f, 0.57f, 0.68f, 1f);
    private static readonly Vector4 ErrorText  = new(1f,    0.45f, 0.42f, 1f);
    private static readonly Vector4 ErrorFrame = new(0.48f, 0.06f, 0.06f, 1f);

    private readonly PluginConfiguration            _config;
    private readonly DeathrollDiscordWebhookService _webhookService;
    private readonly IPluginLog                     _log;
    private readonly ISharedImmediateTexture        _lobbyTexture;
    private readonly ISharedImmediateTexture        _bracketTexture;

    private string _urlDraft              = string.Empty;
    private string _lastCommittedUrl      = string.Empty;
    private string _usernameDraft         = string.Empty;
    private string _lastCommittedUsername = string.Empty;
    private string _avatarUrlDraft        = string.Empty;
    private string _lastCommittedAvatarUrl = string.Empty;
    private bool   _draftInitialised      = false;

    public DeathrollDiscordWebhookTab(
        PluginConfiguration config,
        DeathrollDiscordWebhookService webhookService,
        IPluginLog log)
    {
        _config         = config;
        _webhookService = webhookService;
        _log            = log;

        var imagesDir = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.DirectoryName!,
            "Images");
        _lobbyTexture   = MiniGamesEmporium.TextureProvider.GetFromFile(
            Path.Combine(imagesDir, "drt-example-lobby.png"));
        _bracketTexture = MiniGamesEmporium.TextureProvider.GetFromFile(
            Path.Combine(imagesDir, "drt-example-bracket.png"));
    }

    public void Draw()
    {
        EnsureDraftInitialised();

        ImGuiHelpers.ScaledDummy(8f);
        DrawSectionHeader("Webhook setup");
        DrawSetupGuide();
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(6f);

        if (Entry.PostFailed)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ErrorText))
                ImGui.TextWrapped(
                    "The last delivery failed. Toggle Enable off then on to retry. "
                        + "If the Discord message was deleted a new one will be created.");
            ImGui.Spacing();
        }

        DrawSectionHeader("Webhook URL");
        DrawWebhookRow();

        ImGuiHelpers.ScaledDummy(6f);
        DrawSectionHeader("Webhook appearance (Delete original webhook to reset)");
        DrawAppearanceRows();

        ImGuiHelpers.ScaledDummy(10f);
        DrawSectionHeader("What this posts");
        DrawPostDescription();

        ImGuiHelpers.ScaledDummy(10f);
        DrawSectionHeader("Discord Preview");
        DrawPreviewPair();
    }

    private DeathrollTournamentDiscordEntry Entry => _config.DeathrollTournament.Discord;

    private void DrawWebhookRow()
    {
        using var id = ImRaii.PushId("DRDiscordRow");

        var enabledFlag = Entry.Enabled;

        using (ImRaii.PushColor(ImGuiCol.Text, ErrorText, Entry.PostFailed))
        {
            if (ImGui.Checkbox("##DRDiscordEnabled", ref enabledFlag))
                ToggleEnabled(enabledFlag);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Entry.PostFailed
                    ? "Delivery failed: toggle Enable off then on to retry."
                    : "Paste the webhook URL below, then enable to start posting.");
        }

        ImGui.SameLine();

        using (ImRaii.PushColor(ImGuiCol.FrameBg, ErrorFrame, Entry.PostFailed))
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputText("##DRDiscordUrl", ref _urlDraft, UrlBufferLength);
        }

        if (ImGui.IsItemDeactivatedAfterEdit())
            CommitUrl();

        ImGui.Spacing();
    }

    private void DrawSetupGuide()
    {
        GuideBullet("In Discord, open channel settings (gear icon) for your tournament announcement channel.");
        GuideBullet("Go to Integrations → Webhooks. Create or select a webhook and copy its URL.");
        GuideBullet("Paste the URL below and tick Enable. The plugin posts and patches the embed automatically.");
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

    private void DrawSectionHeader(string label)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, EmporiumNeonTheme.DeathrollTournamentPink))
            ImGui.TextUnformatted(label);
        ImGuiHelpers.ScaledDummy(2f);
        using (ImRaii.PushColor(ImGuiCol.Separator, EmporiumNeonTheme.DeathrollTournamentPink with { W = 0.5f }))
            ImGui.Separator();
        ImGuiHelpers.ScaledDummy(6f);
    }

    private void DrawAppearanceRows()
    {
        var availW      = ImGui.GetContentRegionAvail().X;
        var labelW      = ImGui.CalcTextSize("Image URL").X + ImGuiHelpers.GlobalScale * 8f;
        var inputW      = availW - labelW - ImGui.GetStyle().ItemSpacing.X;

        using (ImRaii.PushColor(ImGuiCol.Text, MutedText))
            ImGui.TextUnformatted("Name");
        ImGui.SameLine(labelW);
        ImGui.SetNextItemWidth(inputW);
        ImGui.InputText("##DRWebhookName", ref _usernameDraft, UsernameBufferLength);
        if (ImGui.IsItemDeactivatedAfterEdit()) CommitUsername();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Display name shown on the Discord webhook message.");

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

    private void EnsureDraftInitialised()
    {
        if (_draftInitialised) return;
        _urlDraft               = Entry.Url;
        _lastCommittedUrl       = Entry.Url;
        _usernameDraft          = Entry.WebhookUsername;
        _lastCommittedUsername  = Entry.WebhookUsername;
        _avatarUrlDraft         = Entry.WebhookAvatarUrl;
        _lastCommittedAvatarUrl = Entry.WebhookAvatarUrl;
        _draftInitialised       = true;
    }

    private void CommitUrl()
    {
        var trimmed = _urlDraft.Trim();
        _urlDraft = trimmed;
        if (trimmed == _lastCommittedUrl) return;

        Entry.MessageId  = null;
        Entry.PostFailed = false;
        Entry.Url        = trimmed;
        _lastCommittedUrl = trimmed;
        _config.Save();

        KickApply();
    }

    private void CommitUsername()
    {
        var trimmed = _usernameDraft.Trim();
        _usernameDraft = trimmed;
        if (trimmed == _lastCommittedUsername) return;

        Entry.WebhookUsername  = trimmed;
        _lastCommittedUsername = trimmed;
        _config.Save();
        KickApply();
    }

    private void CommitAvatarUrl()
    {
        var trimmed = _avatarUrlDraft.Trim();
        _avatarUrlDraft = trimmed;
        if (trimmed == _lastCommittedAvatarUrl) return;

        Entry.WebhookAvatarUrl   = trimmed;
        _lastCommittedAvatarUrl  = trimmed;
        _config.Save();
        KickApply();
    }

    private void ToggleEnabled(bool desired)
    {
        CommitUrl();
        Entry.Enabled = desired;
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
