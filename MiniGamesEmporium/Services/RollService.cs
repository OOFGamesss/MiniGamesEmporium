using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

/// <summary>Central service that parses /random, /dice party and /dice alliance rolls into a single roll result for every game.</summary>

namespace MiniGamesEmporium.Services;

public readonly record struct RollInfo(string PlayerName, int RollValue, int RollMax);

public sealed class RollService
{
    private readonly PlayerInfoService playerInfo;

    public RollService(PlayerInfoService playerInfo)
    {
        this.playerInfo = playerInfo;
    }

    public bool TryParseRoll(XivChatType kind, SeString message, SeString? sender, out RollInfo roll)
    {
        roll = default;
        return kind switch
        {
            XivChatType.RandomNumber => TryParseRandom(message, sender, out roll),
            XivChatType.Party        => TryParseDice(message, sender, out roll),
            XivChatType.Alliance     => TryParseDice(message, sender, out roll),
            _                        => false,
        };
    }

    private bool TryParseRandom(SeString message, SeString? sender, out RollInfo roll)
    {
        roll = default;

        if (!IsGenuineRoll(message)) return false;

        int n1 = 0, n2 = 0, found = 0;
        foreach (var tp in message.Payloads.OfType<TextPayload>())
        {
            var text = tp.Text ?? string.Empty;
            var idx  = 0;
            while (idx < text.Length && found < 2)
            {
                while (idx < text.Length && !char.IsDigit(text[idx])) idx++;
                if (idx >= text.Length) break;
                var start = idx;
                while (idx < text.Length && char.IsDigit(text[idx])) idx++;
                if (int.TryParse(text.AsSpan(start, idx - start), out var num))
                {
                    if (found == 0) n1 = num;
                    else            n2 = num;
                    found++;
                }
            }
            if (found >= 2) break;
        }

        if (found == 0) return false;

        int rollValue, rollMax = 0;
        if (found == 1)
        {
            rollValue = n1;
        }
        else if (n1 <= n2)
        {
            rollValue = n1;
            rollMax   = n2;
        }
        else
        {
            rollValue = n2;
            rollMax   = n1;
        }

        roll = new RollInfo(ResolveName(message, sender), rollValue, rollMax);
        return true;
    }

    private bool TryParseDice(SeString message, SeString? sender, out RollInfo roll)
    {
        roll = default;

        if (!IsGenuineRoll(message)) return false;

        TextPayload? lastText = null;
        foreach (var p in message.Payloads)
            if (p is TextPayload tp) lastText = tp;

        if (lastText == null) return false;

        var lastInts = ExtractIntegers(lastText.Text ?? string.Empty);
        if (lastInts.Count == 0) return false;

        int rollValue, rollMax;
        if (lastInts.Count >= 3)
        {
            rollMax   = lastInts[^2];
            rollValue = lastInts[^1];
        }
        else
        {
            rollValue = lastInts[0];
            rollMax   = lastInts.Count >= 2 ? lastInts[1] : 0;

            if (rollMax == 0)
            {
                foreach (var p in message.Payloads)
                {
                    if (p is TextPayload tp && tp != lastText)
                    {
                        var rangeInts = ExtractIntegers(tp.Text ?? string.Empty);
                        if (rangeInts.Count >= 2) { rollMax = rangeInts[^1]; break; }
                    }
                }
            }
        }

        if (rollValue <= 0) return false;

        roll = new RollInfo(ResolveName(message, sender), rollValue, rollMax);
        return true;
    }

    private static bool IsGenuineRoll(SeString message) =>
        message.Payloads.Any(p => p.Type == PayloadType.Icon);

    private string ResolveName(SeString message, SeString? sender)
    {
        var player = message.Payloads.OfType<PlayerPayload>().FirstOrDefault()
            ?? sender?.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        return player?.PlayerName ?? this.playerInfo.HostName;
    }

    private static List<int> ExtractIntegers(string text)
    {
        var result = new List<int>();
        var i = 0;
        while (i < text.Length)
        {
            if (char.IsDigit(text[i]))
            {
                var start = i;
                while (i < text.Length && char.IsDigit(text[i])) i++;
                if (int.TryParse(text.AsSpan(start, i - start), out var val))
                    result.Add(val);
            }
            else i++;
        }
        return result;
    }
}
