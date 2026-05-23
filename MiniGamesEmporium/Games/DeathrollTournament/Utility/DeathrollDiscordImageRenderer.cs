using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniGamesEmporium.Games.DeathrollTournament.State;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

/// <summary>Generates PNG images for Discord embeds using SixLabors: a paid-player registration card and a live tournament bracket.</summary>

namespace MiniGamesEmporium.Games.DeathrollTournament.Utility;
public static class DeathrollDiscordImageRenderer
{
    private static readonly Color BgColor        = Color.FromRgb(13,  10,  20);
    private static readonly Color HeaderBgColor  = Color.FromRgb(24,   8,  44);
    private static readonly Color SeparatorColor = Color.FromRgb(184, 15,  92);
    private static readonly Color AccentPink     = Color.FromRgb(184, 15,  92);
    private static readonly Color AccentCyan     = Color.FromRgb(34, 211, 238);
    private static readonly Color AccentGold     = Color.FromRgb(240, 192,  48);
    private static readonly Color TextPrimary    = Color.FromRgb(240, 235, 250);
    private static readonly Color TextGreen      = Color.FromRgb(31,  204,  51);
    private static readonly Color TextMuted      = Color.FromRgb(102,  96, 119);
    private static readonly Color TextGray       = Color.FromRgb(150, 140, 170);
    private static readonly Color CardDefault    = Color.FromRgb(20,  12,  36);
    private static readonly Color CardCurrent    = Color.FromRgb(40,  30,   5);
    private static readonly Color CardResolved   = Color.FromRgb( 8,  18,   8);
    private static readonly Color ConnectorColor = Color.FromRgb(60,  30,  80);
    private static readonly Color BorderCurrent  = Color.FromRgb(220, 180,  20);
    private static readonly Color BorderResolved = Color.FromRgb(31,  204,  51);

    private const int CanvasWidth   = 900;
    private const int HeaderHeight  = 90;
    private const int StatsHeight   = 60;
    private const int RowHeight     = 54;
    private const int FooterHeight  = 44;
    private const int PadX         = 28;

    private const int BracketColW   = 220;
    private const int BracketLPad   = 30;
    private const int BracketHdrH   = 70;
    private const int MatchBoxW     = 196;
    private const int MatchBoxH     = 60;
    private const int MatchSlotH    = 70;

    /// <summary>Renders the registration-phase player list card. Only paid players are listed.</summary>
    public static byte[] RenderPlayerList(
        IReadOnlyList<string> paidPlayers,
        long entryCost,
        long boostedPot,
        int registeredCount)
    {
        var twoCol     = paidPlayers.Count > 10;
        var rowsDrawn  = twoCol ? (int)Math.Ceiling(paidPlayers.Count / 2.0) : paidPlayers.Count;
        var playerArea = Math.Max(100, rowsDrawn * RowHeight);
        var height     = HeaderHeight + StatsHeight + 2 + playerArea + FooterHeight;

        using var img = new Image<Rgba32>(CanvasWidth, height);
        img.Mutate(ctx =>
        {
            ctx.Fill(BgColor);
            DrawPlayerListHeader(ctx);
            DrawStatsBar(ctx, entryCost, boostedPot, paidPlayers.Count, registeredCount);
            ctx.Fill(SeparatorColor, new RectangleF(0, HeaderHeight + StatsHeight, CanvasWidth, 2));
            DrawPlayerRows(ctx, paidPlayers, HeaderHeight + StatsHeight + 2);
            DrawFooter(ctx, height);
        });

        return EncodePng(img);
    }

    /// <summary>Renders the full tournament bracket card from the live session state.</summary>
    public static byte[] RenderBracket(DeathrollTournamentState state)
    {
        var roundCount       = state.Rounds.Count;
        var firstRoundCount  = roundCount > 0 ? state.Rounds[0].Count : 1;

        var canvasW = BracketLPad + roundCount * BracketColW + BracketLPad;
        var canvasH = BracketHdrH + firstRoundCount * MatchSlotH + 30;

        using var img = new Image<Rgba32>(canvasW, canvasH);
        img.Mutate(ctx =>
        {
            ctx.Fill(BgColor);
            DrawBracketTitle(ctx, state, canvasW);
            DrawBracketConnectors(ctx, state, firstRoundCount);
            DrawBracketMatches(ctx, state, firstRoundCount);
        });

        return EncodePng(img);
    }

    private static void DrawPlayerListHeader(IImageProcessingContext ctx)
    {
        ctx.Fill(HeaderBgColor, new RectangleF(0, 0, CanvasWidth, HeaderHeight));

        var titleFont    = GetFont(22, FontStyle.Bold);
        var subtitleFont = GetFont(13, FontStyle.Regular);

        DrawCentred(ctx, "DEATHROLL TOURNAMENT", titleFont, AccentPink,
            CanvasWidth / 2f, 18f);
        DrawCentred(ctx, "Player Registration", subtitleFont, TextGray,
            CanvasWidth / 2f, 52f);

        ctx.Fill(AccentPink, new RectangleF(PadX, HeaderHeight - 3, CanvasWidth - PadX * 2, 2));
    }

    private static void DrawStatsBar(
        IImageProcessingContext ctx,
        long entryCost, long boostedPot,
        int paidCount, int registeredCount)
    {
        var y         = HeaderHeight + 2f;
        var statFont  = GetFont(13, FontStyle.Regular);
        var boldFont  = GetFont(13, FontStyle.Bold);
        var pot       = entryCost * paidCount + boostedPot;

        float[] xs  = { CanvasWidth * 0.17f, CanvasWidth * 0.50f, CanvasWidth * 0.83f };
        string[] labels = { "Entry Cost", "Players", "Current Pot" };
        string[] values =
        {
            entryCost == 0 ? "Free" : $"{entryCost:N0} Gil",
            $"{paidCount}",
            $"{pot:N0} Gil",
        };
        Color[] valueColors = { AccentCyan, AccentPink, AccentGold };

        for (var i = 0; i < 3; i++)
        {
            DrawCentred(ctx, labels[i], statFont, TextMuted, xs[i], y + 8f);
            DrawCentred(ctx, values[i], boldFont, valueColors[i], xs[i], y + 28f);
        }
    }

    private static void DrawPlayerRows(IImageProcessingContext ctx, IReadOnlyList<string> players, int startY)
    {
        if (players.Count == 0)
        {
            var emptyFont = GetFont(14, FontStyle.Regular);
            DrawCentred(ctx, "No paid players yet.", emptyFont, TextMuted,
                CanvasWidth / 2f, startY + 30f);
            return;
        }

        var twoCol    = players.Count > 10;
        var colCount  = twoCol ? 2 : 1;
        var colWidth  = (CanvasWidth - PadX * 2) / colCount;
        var nameFont  = GetFont(14, FontStyle.Bold);
        var numFont   = GetFont(12, FontStyle.Regular);
        var midRows   = (int)Math.Ceiling(players.Count / (double)colCount);

        for (var i = 0; i < players.Count; i++)
        {
            var col   = twoCol ? i / midRows : 0;
            var row   = twoCol ? i % midRows : i;
            var x     = PadX + col * colWidth;
            var y     = startY + row * RowHeight;
            var rowBg = i % 2 == 0
                ? Color.FromRgba(255, 255, 255, 6)
                : Color.FromRgba(0, 0, 0, 0);

            if (i % 2 == 0)
                ctx.Fill(rowBg, new RectangleF(x, y, colWidth, RowHeight));

            DrawLeft(ctx, $"#{i + 1}", numFont, TextMuted, x + 8f, y + (RowHeight - 14f) / 2f);

            var nameX = x + 42f;
            DrawLeft(ctx, players[i], nameFont, TextPrimary, nameX, y + (RowHeight - 16f) / 2f);
        }
    }

    private static void DrawFooter(IImageProcessingContext ctx, int canvasH)
    {
        var y        = canvasH - FooterHeight;
        var footFont = GetFont(11, FontStyle.Regular);
        ctx.Fill(HeaderBgColor, new RectangleF(0, y, CanvasWidth, FooterHeight));
        ctx.Fill(AccentPink, new RectangleF(0, y, CanvasWidth, 1));
        DrawCentred(ctx, "Mini Games Emporium Plogon", footFont, TextMuted,
            CanvasWidth / 2f, y + 14f);
    }

    private static void DrawBracketTitle(IImageProcessingContext ctx, DeathrollTournamentState state, int canvasW)
    {
        ctx.Fill(HeaderBgColor, new RectangleF(0, 0, canvasW, BracketHdrH));
        var titleFont = GetFont(20, FontStyle.Bold);
        var subFont   = GetFont(12, FontStyle.Regular);

        var title = state.TournamentWinner != null
            ? $"TOURNAMENT COMPLETE  -  {ParseName(state.TournamentWinner)} WINS!"
            : "DEATHROLL TOURNAMENT  -  LIVE BRACKET";
        var titleColor = state.TournamentWinner != null ? AccentGold : AccentPink;

        DrawCentred(ctx, title, titleFont, titleColor, canvasW / 2f, 10f);

        var roundFont = GetFont(12, FontStyle.Bold);
        for (var r = 0; r < state.Rounds.Count; r++)
        {
            var bestOf = r < state.BestOfPerRound.Count ? state.BestOfPerRound[r] : 1;
            var label  = r == state.Rounds.Count - 1 ? "FINAL" : $"ROUND {r + 1}";
            var colCX  = BracketLPad + r * BracketColW + BracketColW / 2f;
            DrawCentred(ctx, label, roundFont, TextGray, colCX, 38f);
            DrawCentred(ctx, $"BO{bestOf}", GetFont(10, FontStyle.Regular), TextMuted, colCX, 54f);
        }

        ctx.Fill(AccentPink, new RectangleF(0, BracketHdrH - 1, canvasW, 1));
    }

    private static void DrawBracketConnectors(
        IImageProcessingContext ctx,
        DeathrollTournamentState state,
        int firstRoundCount)
    {
        var pen = new SolidPen(ConnectorColor, 1.5f);

        for (var r = 0; r < state.Rounds.Count - 1; r++)
        {
            var matches = state.Rounds[r];
            var span    = firstRoundCount / matches.Count;

            for (var m = 0; m < matches.Count; m++)
            {
                var matchTopY  = BracketHdrH + m * span * MatchSlotH + (span * MatchSlotH - MatchBoxH) / 2f;
                var matchCY    = matchTopY + MatchBoxH / 2f;
                var rightX     = BracketLPad + r * BracketColW + MatchBoxW;

                var stubEndX = BracketLPad + (r + 1) * BracketColW - 2f;
                ctx.DrawLine(pen, new PointF(rightX, matchCY), new PointF(stubEndX, matchCY));
            }

            for (var m = 0; m < matches.Count; m += 2)
            {
                if (m + 1 >= matches.Count) break;
                var span1  = firstRoundCount / matches.Count;
                var cy1    = BracketHdrH + m * span1 * MatchSlotH       + (span1 * MatchSlotH - MatchBoxH) / 2f + MatchBoxH / 2f;
                var cy2    = BracketHdrH + (m + 1) * span1 * MatchSlotH + (span1 * MatchSlotH - MatchBoxH) / 2f + MatchBoxH / 2f;
                var midX   = BracketLPad + (r + 1) * BracketColW - 2f;
                ctx.DrawLine(pen, new PointF(midX, cy1), new PointF(midX, cy2));
            }
        }
    }

    private static void DrawBracketMatches(
        IImageProcessingContext ctx,
        DeathrollTournamentState state,
        int firstRoundCount)
    {
        for (var r = 0; r < state.Rounds.Count; r++)
        {
            var matches = state.Rounds[r];
            var span    = firstRoundCount / Math.Max(1, matches.Count);

            for (var m = 0; m < matches.Count; m++)
            {
                var match     = matches[m];
                var isCurrent = r == state.CurrentRoundIndex && m == state.CurrentMatchIndex;
                var boxX      = BracketLPad + r * BracketColW;
                var boxY      = BracketHdrH + m * span * MatchSlotH + (span * MatchSlotH - MatchBoxH) / 2f;

                DrawMatchBox(ctx, state, match, isCurrent, boxX, boxY);
            }
        }
    }

    private static void DrawMatchBox(
        IImageProcessingContext ctx,
        DeathrollTournamentState state,
        BracketMatch match,
        bool isCurrent,
        float x, float y)
    {
        var bg = match.IsResolved ? CardResolved : isCurrent ? CardCurrent : CardDefault;
        ctx.Fill(bg, new RectangleF(x, y, MatchBoxW, MatchBoxH));

        var borderColor = match.IsResolved ? BorderResolved : isCurrent ? BorderCurrent : ConnectorColor;
        var borderW     = match.IsResolved || isCurrent ? 1.5f : 1f;
        ctx.Draw(new SolidPen(borderColor, borderW), new RectangleF(x, y, MatchBoxW, MatchBoxH));

        var p1Display = FormatSlot(match.Player1);
        var p2Display = FormatSlot(match.Player2);

        var p1Wins = isCurrent && !match.IsResolved ? state.ActiveMatchPlayer1Wins : match.Player1Wins;
        var p2Wins = isCurrent && !match.IsResolved ? state.ActiveMatchPlayer2Wins : match.Player2Wins;

        var p1Color = GetBracketNameColor(match, isP1: true,  isCurrent, state);
        var p2Color = GetBracketNameColor(match, isP1: false, isCurrent, state);

        var nameFont  = GetFont(12, FontStyle.Bold);
        var scoreFont = GetFont(10, FontStyle.Regular);
        var vsFont    = GetFont(10, FontStyle.Regular);

        var innerX  = x + 8f;
        var innerW  = MatchBoxW - 16f;
        var scoreX  = x + MatchBoxW - 26f;

        var p1Y = y + 10f;
        var vsY = y + (MatchBoxH - 12f) / 2f;
        var p2Y = y + MatchBoxH - 24f;

        DrawLeft(ctx, TruncateName(p1Display, nameFont, innerW - 28f), nameFont, p1Color, innerX, p1Y);
        if (p1Wins > 0 || p2Wins > 0)
            DrawLeft(ctx, p1Wins.ToString(), scoreFont, p1Color, scoreX, p1Y + 2f);

        DrawCentred(ctx, "vs", vsFont, TextMuted, x + MatchBoxW / 2f, vsY);

        DrawLeft(ctx, TruncateName(p2Display, nameFont, innerW - 28f), nameFont, p2Color, innerX, p2Y);
        if (p1Wins > 0 || p2Wins > 0)
            DrawLeft(ctx, p2Wins.ToString(), scoreFont, p2Color, scoreX, p2Y + 2f);
    }

    private static Color GetBracketNameColor(BracketMatch match, bool isP1, bool isCurrent, DeathrollTournamentState state)
    {
        var player = isP1 ? match.Player1 : match.Player2;
        if (string.IsNullOrEmpty(player)) return TextPrimary;
        if (DeathrollGameIds.IsBye(player)) return TextPrimary;
        if (match.IsResolved && string.Equals(match.Winner, player, StringComparison.OrdinalIgnoreCase))
            return TextGreen;
        if (match.IsResolved) return TextMuted;
        if (isCurrent && !string.IsNullOrEmpty(state.CurrentTurnPlayerName))
        {
            if (ParseName(player).Equals(ParseName(state.CurrentTurnPlayerName), StringComparison.OrdinalIgnoreCase))
                return AccentGold;
        }
        return TextPrimary;
    }

    private static void DrawCentred(IImageProcessingContext ctx, string text, Font font, Color color, float cx, float y)
    {
        ctx.DrawText(new RichTextOptions(font)
        {
            Origin = new System.Numerics.Vector2(cx, y),
            HorizontalAlignment = HorizontalAlignment.Center,
        }, text, color);
    }

    private static void DrawLeft(IImageProcessingContext ctx, string text, Font font, Color color, float x, float y)
    {
        ctx.DrawText(new RichTextOptions(font)
        {
            Origin = new System.Numerics.Vector2(x, y),
            HorizontalAlignment = HorizontalAlignment.Left,
        }, text, color);
    }

    private static Font GetFont(float size, FontStyle style)
    {
        foreach (var name in new[] { "Segoe UI", "Arial", "Liberation Sans" })
        {
            if (SystemFonts.TryGet(name, out var family))
                return family.CreateFont(size, style);
        }
        return SystemFonts.Families.First().CreateFont(size, style);
    }

    private static string TruncateName(string text, Font font, float maxWidth)
    {
        var opts = new TextOptions(font);
        if (TextMeasurer.MeasureSize(text, opts).Width <= maxWidth) return text;
        const string ellipsis = "…";
        var budget = maxWidth - TextMeasurer.MeasureSize(ellipsis, opts).Width;
        while (text.Length > 0 && TextMeasurer.MeasureSize(text, opts).Width > budget)
            text = text[..^1];
        return text + ellipsis;
    }

    private static string FormatSlot(string slot)
    {
        if (string.IsNullOrEmpty(slot)) return "TBD";
        if (DeathrollGameIds.IsBye(slot)) return "BYE";
        return ParseName(slot);
    }

    private static string ParseName(string entry)
    {
        var at = entry.IndexOf('@');
        return at < 0 ? entry.Trim() : entry[..at].Trim();
    }

    private static byte[] EncodePng(Image<Rgba32> img)
    {
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }
}
