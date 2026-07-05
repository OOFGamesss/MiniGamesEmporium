using System.Collections.Generic;

/// <summary>IPC v2 rules contract mirrored to GambaWhere when pushing live session state.</summary>

namespace MiniGamesEmporium.IPC;

public sealed class GambaWhereRulesPayload
{
    public List<GambaWhereRuleEntry> Rules { get; set; } = new();
}

public sealed class GambaWhereRuleEntry
{
    public string Label { get; set; } = string.Empty;

    public object? Value { get; set; }
}
