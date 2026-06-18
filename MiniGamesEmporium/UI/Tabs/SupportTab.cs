using System;
using System.IO;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using MiniGamesEmporium.UI.Components;

namespace MiniGamesEmporium.UI.Tabs;

public class SupportTab
{
    private readonly ISharedImmediateTexture? _logo;

    private const string OofGamesDiscordUrl = "https://discord.gg/vM6ff4h5Ym";
    private const string OofGamesWebsiteUrl = "https://oofgames.fyi";

    private const string DiscordJoinLine = "Join the OOF Games Discord ";
    private const string WebsiteVisitLine = "Visit the OOF Games Website ";
    private const float LogoSide = 160f;

    public SupportTab()
    {
        var logoPath = Path.Combine(
            MiniGamesEmporium.PluginInterface.AssemblyLocation.Directory?.FullName ?? string.Empty,
            "Images", "oofgames.png");
        if (File.Exists(logoPath))
            _logo = MiniGamesEmporium.TextureProvider.GetFromFile(logoPath);
    }

    public void Draw()
    {
        DrawBranding();
        ImGuiHelpers.ScaledDummy(6f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(8f);
        DrawSupportGuidance();
        DrawDiscordInvite();
    }

    private void DrawBranding()
    {
        var side = LogoSide * ImGuiHelpers.GlobalScale;
        var logoDrawSize = new Vector2(side, side);
        CentreForWidth(logoDrawSize.X);

        var tex = _logo?.GetWrapOrDefault();
        if (tex != null)
            ImGui.Image(tex.Handle, logoDrawSize);
        else
            DrawLogoPlaceholder(logoDrawSize);

        ImGuiHelpers.ScaledDummy(4f);
        const string attribution = "Created by OOF Games";
        CentreForWidth(ImGui.CalcTextSize(attribution).X);
        ImGui.TextUnformatted(attribution);
    }

    private static void DrawSupportGuidance()
    {
        DrawFaqSection(
            "##faq_bug",
            "How do I report a bug?",
            "Post in #report_bugs with a clear description of what you were doing when the problem occurred. Screenshots help us reproduce and fix issues.");

        DrawFaqSection(
            "##faq_feature",
            "How do I request a feature?",
            "Use #request_features and explain what you would like added, plus any context that helps us understand the request.");

        DrawFaqSection(
            "##faq_software",
            "Can I request a plugin, Discord bot or website?",
            "For a similar plugin, a Discord bot, another plugin type or a website, post in #request_software and we will see what we can do.");
    }

    private static void DrawFaqSection(string idSuffix, string heading, string body)
    {
        ImGui.PushStyleColor(ImGuiCol.Header,        new Vector4(0.22f, 0.07f, 0.36f, 0.80f));
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, EmporiumNeonTheme.MainTabPurpleHovered);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive,  EmporiumNeonTheme.MainTabPurpleActive);
        ImGui.PushStyleColor(ImGuiCol.Text,          EmporiumNeonTheme.NeonCyan);

        if (ImGui.CollapsingHeader($"{heading}{idSuffix}", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PopStyleColor(4);
            ImGui.TextWrapped(body);
        }
        else
        {
            ImGui.PopStyleColor(4);
        }

        ImGuiHelpers.ScaledDummy(6f);
    }

    private static void DrawDiscordInvite()
    {
        var style     = ImGui.GetStyle();
        var rowH      = ImGui.GetFrameHeight();
        var avail     = ImGui.GetContentRegionAvail();
        var padBottom = 2f * ImGuiHelpers.GlobalScale;
        var liftUp    = 14f * ImGuiHelpers.GlobalScale;
        var rowsH     = (rowH * 2f) + style.ItemSpacing.Y;
        var spareY    = Math.Max(0f, avail.Y - rowsH - padBottom - liftUp);
        if (spareY > 0f)
            ImGui.Dummy(new Vector2(1f, spareY));

        DrawLinkRow("##openOofDiscord", DiscordJoinLine, OofGamesDiscordUrl);
        DrawLinkRow("##openOofWebsite", WebsiteVisitLine, OofGamesWebsiteUrl);
    }

    private static void DrawLinkRow(string id, string label, string url)
    {
        var style = ImGui.GetStyle();
        var btnW  = ImGui.GetFrameHeight();
        var rowW  = ImGui.CalcTextSize(label).X + style.ItemSpacing.X + btnW;
        CentreForWidth(rowW);

        ImGui.BeginGroup();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine();
        if (ImGuiComponents.IconButton(id, FontAwesomeIcon.Globe))
            Util.OpenLink(url);

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Open in browser:\n{url}");

        ImGui.EndGroup();
    }

    private static void CentreForWidth(float width)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, (avail - width) * 0.5f));
    }

    private static void DrawLogoPlaceholder(Vector2 size)
    {
        var pos = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddRectFilled(
            pos,
            pos + size,
            ImGui.GetColorU32(new Vector4(0.18f, 0.18f, 0.18f, 1f)),
            4f * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(size);
    }
}
