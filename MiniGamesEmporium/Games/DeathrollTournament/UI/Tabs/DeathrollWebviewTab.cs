using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Webview;
using MiniGamesEmporium.UI.Components;
using MiniGamesEmporium.Utility;

/// <summary>Draws the Deathroll Tournament Webview tab: venue setup and the live web session controls.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.UI.Tabs;
public sealed class DeathrollWebviewTab
{
    private static readonly Vector4 GreenButton        = new(0.04f, 0.42f, 0.16f, 1f);
    private static readonly Vector4 GreenButtonHovered = new(0.06f, 0.58f, 0.22f, 1f);
    private static readonly Vector4 GreenButtonActive  = new(0.10f, 0.70f, 0.28f, 1f);
    private static readonly Vector4 RedButton          = new(0.52f, 0.08f, 0.10f, 1f);
    private static readonly Vector4 RedButtonHovered   = new(0.68f, 0.12f, 0.14f, 1f);
    private static readonly Vector4 RedButtonActive    = new(0.80f, 0.18f, 0.20f, 1f);
    private static readonly Vector4 Positive           = new(0.35f, 0.95f, 0.50f, 1f);
    private static readonly Vector4 Negative           = new(1.00f, 0.35f, 0.35f, 1f);
    private static readonly Vector4 Gold               = new(1.00f, 0.85f, 0.35f, 1f);

    private readonly PluginConfiguration config;
    private readonly DrtWebviewService webviewService;

    public DeathrollWebviewTab(PluginConfiguration config, DrtWebviewService webviewService)
    {
        this.config         = config;
        this.webviewService = webviewService;
    }

    public void Draw()
    {
        ImGui.Spacing();
        ImGui.TextColored(EmporiumNeonTheme.DeathrollTournamentPink, "Webview");
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Mirror this tournament to the OOF Games website so anyone can watch the bracket live.");
        ImGui.Spacing();

        if (string.IsNullOrWhiteSpace(this.config.Webview.ApiHostKey))
        {
            ImGui.TextColored(EmporiumNeonTheme.WarnAmber, "Enter your API key in the plugin Settings tab first.");
            return;
        }

        var drt        = this.config.DeathrollTournament;
        var live       = this.webviewService.SessionId != null;
        var nameCheck  = VenueValidator.ValidateName(drt.WebVenueName);
        var imageCheck = VenueValidator.ValidateImageUrl(drt.WebVenueImageUrl);

        if (!live)
        {
            var venueName = drt.WebVenueName;
            ImGui.TextUnformatted("Venue Name:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(320);
            if (ImGui.InputText("##dr_venue_name", ref venueName, 64))
            {
                drt.WebVenueName = venueName;
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("(max 40 characters, shown on the website)");
            if (!nameCheck.Ok) ImGui.TextColored(EmporiumNeonTheme.WarnAmber, nameCheck.Error);

            var venueImage = drt.WebVenueImageUrl;
            ImGui.TextUnformatted("Venue Image URL:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(320);
            if (ImGui.InputText("##dr_venue_image", ref venueImage, 512))
            {
                drt.WebVenueImageUrl = venueImage.Trim();
                this.config.Save();
            }
            ImGui.SameLine();
            ImGui.TextDisabled("(.jpg/.png/.webp, max 2048x2048, no Imgur)");
            if (!imageCheck.Ok) ImGui.TextColored(EmporiumNeonTheme.WarnAmber, imageCheck.Error);
        }

        ImGui.Spacing();
        if (!live)
        {
            var blocked = !nameCheck.Ok || !imageCheck.Ok;
            using var disabled = ImRaii.Disabled(blocked);
            ImGui.PushStyleColor(ImGuiCol.Button,        GreenButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, GreenButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  GreenButtonActive);
            var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Globe, "Go Live", "##dr_go_live");
            ImGui.PopStyleColor(3);
            if (clicked) this.webviewService.GoLive();
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button,        RedButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, RedButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,  RedButtonActive);
            var clicked = UIHelper.IconTextButton(FontAwesomeIcon.Stop, "End Web Session", "##dr_end_web");
            ImGui.PopStyleColor(3);
            if (clicked) this.webviewService.EndSession();
        }

        ImGui.SameLine();
        var statusColour = this.webviewService.StatusIsError ? Negative
            : this.webviewService.Connected ? Positive
            : live ? EmporiumNeonTheme.WarnAmber : new Vector4(0.6f, 0.6f, 0.6f, 1f);
        ImGui.TextColored(statusColour, $"Status: {this.webviewService.Status}");

        if (live)
        {
            ImGui.Spacing();
            ImGui.TextUnformatted("Session Code:");
            ImGui.SameLine();
            ImGui.TextColored(Gold, this.webviewService.SessionId ?? string.Empty);
            ImGui.SameLine();
            if (UIHelper.IconTextButton(FontAwesomeIcon.Copy, "Copy Link", "##dr_copy_link"))
                ImGui.SetClipboardText(this.webviewService.SpectatorUrl ?? string.Empty);
            ImGui.TextDisabled(this.webviewService.SpectatorUrl ?? string.Empty);
            ImGui.Spacing();
            ImGui.TextWrapped("Share the link with spectators. While registration is open, web visitors can request to join; their names appear under Web Requests in the Game tab for you to accept or reject.");
        }
    }
}
