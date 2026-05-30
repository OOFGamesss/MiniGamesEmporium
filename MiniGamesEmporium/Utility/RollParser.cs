using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;
using System.Linq;

/// <summary>Parses a RandomNumber chat message into a player name, roll value, and roll max.</summary>

namespace MiniGamesEmporium.Utility;
public static class RollParser
{
    public static bool TryParse(SeString seString, out string playerName, out int rollValue, out int rollMax)
    {
        playerName = string.Empty;
        rollValue  = 0;
        rollMax    = 0;

        var playerPayload = seString.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        if (playerPayload != null)
            playerName = playerPayload.PlayerName;

        int n1 = 0, n2 = 0, found = 0;
        foreach (var tp in seString.Payloads.OfType<TextPayload>())
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

        if (found == 1)
        {
            rollValue = n1;
        }
        else
        {
            if (n1 <= n2) { rollValue = n1; rollMax = n2; }
            else          { rollValue = n2; rollMax = n1; }
        }

        return true;
    }
}
