/// <summary>Identifiers used when publishing to GambaWhere.</summary>

namespace MiniGamesEmporium.IPC;

public static class GambaWhereIds
{
    public const string PluginName = "Mini Games Emporium";
    public const string Category   = "Mini Games";
    public const int MaxRules = 10;

    private const string RulesPrefix = "MGE";

    public static string PluginNameFor(string gameDisplayName) => $"{RulesPrefix} {gameDisplayName}";
}
