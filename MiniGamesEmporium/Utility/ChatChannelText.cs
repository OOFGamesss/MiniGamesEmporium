/// <summary>Shared helpers for rewriting the leading chat channel token on outgoing messages and templates.</summary>

namespace MiniGamesEmporium.Utility;
public static class ChatChannelText
{
    public static string ApplyPartyChannel(string message, string? target) =>
        string.IsNullOrEmpty(target) ? message : ConvertLeadingChannel(message, target);

    public static string ConvertLeadingChannelPerLine(string message, string target)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var lines = message.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
            lines[i] = ConvertLeadingChannel(lines[i], target);
        return string.Join("\n", lines);
    }

    public static string ConvertLeadingChannel(string message, string target)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        int split = message.IndexOfAny(new[] { ' ', '\t' });
        string token = split < 0 ? message : message.Substring(0, split);
        string rest = split < 0 ? string.Empty : message.Substring(split);

        switch (token.ToLowerInvariant())
        {
            case "/s":
            case "/say":
            case "/p":
            case "/party":
            case "/a":
            case "/alliance":
                return target + rest;
            default:
                return message;
        }
    }
}
