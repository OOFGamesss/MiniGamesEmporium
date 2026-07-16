using System;

/// <summary>Plugin-wide webview settings shared by every game that mirrors to the MiniGames Emporium API.</summary>

namespace MiniGamesEmporium.Config;
[Serializable]
public class WebviewConfig
{
    public string ApiHostKey { get; set; } = string.Empty;
}
