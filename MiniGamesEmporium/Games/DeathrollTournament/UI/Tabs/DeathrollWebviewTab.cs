using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Webview;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.Utility;
using System;
using System.IO;
using System.Numerics;

/// <summary>Draws the Deathroll Tournament Webview tab for the live web spectator session.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollWebviewTab
{
    private const float FieldWidth = 320f;

    private static readonly Vector4 Positive = new(0.35f, 0.95f, 0.50f, 1f);
    private static readonly Vector4 Negative = new(1.00f, 0.35f, 0.35f, 1f);
    private static readonly Vector4 Muted    = new(0.60f, 0.60f, 0.60f, 1f);
    private static readonly Vector4 Gold     = new(1.00f, 0.85f, 0.35f, 1f);

    private readonly PluginConfiguration config;
    private readonly DrtWebviewService webviewService;
    private readonly ISharedImmediateTexture? previewTexture;

    public DeathrollWebviewTab(PluginConfiguration config, DrtWebviewService webviewService)
    {
        this.config         = config;
        this.webviewService = webviewService;

        var previewPath = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "Screenshots", "drt-webview.png");
        if (File.Exists(previewPath))
            this.previewTexture = MiniGamesEmporium.TextureProvider.GetFromFile(previewPath);
    }

    public void Draw()
    {
        DrawIntroSection();
        DrawVenueSection();
        DrawSessionSection();
    }

    private static void DrawIntroSection()
    {
        UIHelper.SectionHeader("Web Spectator", EmporiumNeonTheme.DeathrollTournamentPink);
        ImGui.TextWrapped(
            "Mirror this tournament to the OOF Games website so anyone can watch the bracket live in a browser.");
        ImGuiHelpers.ScaledDummy(8f);
    }

    private void DrawVenueSection()
    {
        if (this.webviewService.SessionId != null) return;

        UIHelper.SectionHeader("Venue", EmporiumNeonTheme.DeathrollTournamentPink);

        var drt = this.config.DeathrollTournament;
        var scale = ImGuiHelpers.GlobalScale;

        var venueName = drt.WebVenueName;
        ImGui.TextUnformatted("Venue Name:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(FieldWidth * scale);
        if (ImGui.InputText("##dr_venue_name", ref venueName, 64))
        {
            drt.WebVenueName = venueName;
            this.config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(max 40 characters, shown on the website)");

        var nameCheck = VenueValidator.ValidateName(drt.WebVenueName);
        if (!nameCheck.Ok) ImGui.TextColored(EmporiumNeonTheme.WarnAmber, nameCheck.Error);

        var venueImage = drt.WebVenueImageUrl;
        ImGui.TextUnformatted("Venue Image URL:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(FieldWidth * scale);
        if (ImGui.InputText("##dr_venue_image", ref venueImage, 512))
        {
            drt.WebVenueImageUrl = venueImage.Trim();
            this.config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(.jpg/.png/.webp, max 2048x2048, no Imgur)");

        var imageCheck = VenueValidator.ValidateImageUrl(drt.WebVenueImageUrl);
        if (!imageCheck.Ok) ImGui.TextColored(EmporiumNeonTheme.WarnAmber, imageCheck.Error);

        ImGuiHelpers.ScaledDummy(8f);
    }

    private void DrawSessionSection()
    {
        UIHelper.SectionHeader("Session", EmporiumNeonTheme.DeathrollTournamentPink);

        var live = this.webviewService.SessionId != null;

        if (!live)
            DrawGoLiveButton();
        else
            using (UIHelper.PushRedButtonColours())
                if (UIHelper.IconTextButton(FontAwesomeIcon.Stop, "End Web Session", "##dr_end_web"))
                    this.webviewService.EndSession();

        ImGui.SameLine();
        var statusColour = this.webviewService.StatusIsError ? Negative
            : this.webviewService.Connected ? Positive
            : live ? EmporiumNeonTheme.WarnAmber : Muted;
        ImGui.TextColored(statusColour, $"Status: {this.webviewService.Status}");

        if (live)
            DrawLiveSessionDetails();

        DrawPreview();
    }

    private void DrawGoLiveButton()
    {
        var drt = this.config.DeathrollTournament;
        var hasKey = !string.IsNullOrWhiteSpace(this.config.Webview.ApiHostKey);
        var blocked = !hasKey
            || !VenueValidator.ValidateName(drt.WebVenueName).Ok
            || !VenueValidator.ValidateImageUrl(drt.WebVenueImageUrl).Ok;

        using (ImRaii.Disabled(blocked))
        using (UIHelper.PushGreenButtonColours())
            if (UIHelper.IconTextButton(FontAwesomeIcon.Globe, "Go Live", "##dr_go_live"))
                this.webviewService.GoLive();

        if (hasKey) return;

        ImGui.SameLine();
        ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Add your game key in Settings > Webview Setup first.");
    }

    private void DrawLiveSessionDetails()
    {
        ImGui.Spacing();
        ImGui.TextUnformatted("Session Code:");
        ImGui.SameLine();
        ImGui.TextColored(Gold, this.webviewService.SessionId ?? string.Empty);
        ImGui.SameLine();
        if (UIHelper.IconTextButton(FontAwesomeIcon.Copy, "Copy Link", "##dr_copy_link"))
            ImGui.SetClipboardText(this.webviewService.SpectatorUrl ?? string.Empty);

        ImGui.TextDisabled(this.webviewService.SpectatorUrl ?? string.Empty);
        ImGui.TextWrapped(
            "Share the link with spectators. While registration is open, web visitors can request to join; "
                + "their names appear under Web Requests in the Game section for you to accept or reject.");
    }

    private void DrawPreview()
    {
        if (this.previewTexture == null) return;

        ImGuiHelpers.ScaledDummy(10f);
        UIHelper.SectionHeader("Web Preview", EmporiumNeonTheme.DeathrollTournamentPink);

        var imgW   = ImGui.GetContentRegionAvail().X;
        var startX = ImGui.GetCursorPosX();

        if (this.previewTexture.TryGetWrap(out var wrap, out _))
        {
            var imgH = imgW * (wrap.Height / (float)wrap.Width);
            ImGui.Image(wrap.Handle, new Vector2(imgW, imgH));
        }
        else
        {
            ImGui.Dummy(new Vector2(imgW, imgW * 0.66f));
        }

        const string caption = "Example spectator view";
        var textW = ImGui.CalcTextSize(caption).X;
        ImGui.SetCursorPosX(startX + MathF.Max(0f, (imgW - textW) * 0.5f));
        ImGui.TextDisabled(caption);
        ImGuiHelpers.ScaledDummy(8f);
    }
}
