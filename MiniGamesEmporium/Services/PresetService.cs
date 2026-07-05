using MiniGamesEmporium.Config;
using MiniGamesEmporium.Games.Bar777.Config;
using MiniGamesEmporium.Games.DeathrollTournament.Config;
using MiniGamesEmporium.Games.HigherLower.Config;
using MiniGamesEmporium.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

/// <summary>Creates, applies, and manages named plugin presets.</summary>

namespace MiniGamesEmporium.Services;
public sealed class PresetService
{
    private readonly PluginConfiguration config;

    private const int MaxBestOfRounds = 10;

    public PresetService(PluginConfiguration config)
    {
        this.config = config;
        EnsureDefaultPreset();
        SanitiseBestOfRounds();
    }

    private void SanitiseBestOfRounds()
    {
        var changed = false;
        changed |= TrimBestOfList(this.config.DeathrollTournament.BestOfPerRound);
        foreach (var preset in this.config.Presets)
            changed |= TrimBestOfList(preset.DeathrollTournament.BestOfPerRound);
        if (changed) this.config.Save();
    }

    private static bool TrimBestOfList(List<int> list)
    {
        if (list.Count <= MaxBestOfRounds) return false;
        list.RemoveRange(MaxBestOfRounds, list.Count - MaxBestOfRounds);
        return true;
    }

    private void EnsureDefaultPreset()
    {
        if (this.config.Presets.Count > 0) return;
        this.config.Presets.Add(new PluginPreset { Name = "Default" });
        this.config.ActivePresetIndex = 0;
        this.config.Save();
    }

    public void CreatePreset(string name)
    {
        this.config.Presets.Add(CaptureCurrentSettings(name));
        this.config.ActivePresetIndex = this.config.Presets.Count - 1;
        this.config.Save();
    }

    public void ApplyPreset(int index)
    {
        if (index < 0 || index >= this.config.Presets.Count) return;
        var preset = this.config.Presets[index];
        ApplyBar777Settings(preset.Bar777);
        ApplyDeathrollSettings(preset.DeathrollTournament);
        ApplyHigherLowerSettings(preset.HigherLower);
        this.config.QueueKeyword = preset.QueueKeyword;
        this.config.QueueJoinChannels = DeepClone(preset.QueueJoinChannels);
        this.config.ActivePresetIndex = index;
        this.config.Save();
    }

    public void UpdatePreset(int index)
    {
        if (index < 0 || index >= this.config.Presets.Count) return;
        var existing = this.config.Presets[index];
        var updated = CaptureCurrentSettings(existing.Name);
        updated.Id = existing.Id;
        this.config.Presets[index] = updated;
        this.config.Save();
    }

    public void DeletePreset(int index)
    {
        if (this.config.Presets.Count <= 1) return;
        this.config.Presets.RemoveAt(index);
        if (this.config.ActivePresetIndex >= this.config.Presets.Count)
            this.config.ActivePresetIndex = this.config.Presets.Count - 1;
        this.config.Save();
    }

    public void RenamePreset(int index, string newName)
    {
        if (index < 0 || index >= this.config.Presets.Count) return;
        if (string.IsNullOrWhiteSpace(newName)) return;
        this.config.Presets[index].Name = newName;
        this.config.Save();
    }

    private PluginPreset CaptureCurrentSettings(string name) => new()
    {
        Name = name,
        QueueKeyword = this.config.QueueKeyword,
        QueueJoinChannels = DeepClone(this.config.QueueJoinChannels),
        Bar777 = CaptureBar777(),
        DeathrollTournament = CaptureDeathroll(),
        HigherLower = CaptureHigherLower(),
    };

    private Bar777Config CaptureBar777()
    {
        var s = this.config.Bar777;
        return new Bar777Config
        {
            CustomName    = s.CustomName,
            CostPerRoll   = s.CostPerRoll,
            MaxRolls      = s.MaxRolls,
            WinNumber     = s.WinNumber,
            UseQueue      = s.UseQueue,
            AutoCatchRoll = s.AutoCatchRoll,
            Chat          = DeepClone(s.Chat),
        };
    }

    private DeathrollTournamentConfig CaptureDeathroll()
    {
        var s = this.config.DeathrollTournament;
        return new DeathrollTournamentConfig
        {
            EntryCost                 = s.EntryCost,
            BestOfPerRound            = new List<int>(s.BestOfPerRound),
            AutoNextMatch             = s.AutoNextMatch,
            AutoNextMatchDelaySeconds = s.AutoNextMatchDelaySeconds,
            AutoCatchNextRound        = s.AutoCatchNextRound,
            AutoJoinKeyword           = s.AutoJoinKeyword,
            JoinKeyword               = s.JoinKeyword,
            JoinChannels              = DeepClone(s.JoinChannels),
            Chat                      = DeepClone(s.Chat),
            WebhookUsername           = s.WebhookUsername,
            WebhookAvatarUrl          = s.WebhookAvatarUrl,
            DiscordWebhooks           = s.DiscordWebhooks.Select(e => new DeathrollTournamentDiscordEntry
            {
                Alias   = e.Alias,
                Url     = e.Url,
                Enabled = e.Enabled,
            }).ToList(),
        };
    }

    private HigherLowerConfig CaptureHigherLower()
    {
        var s = this.config.HigherLower;
        return new HigherLowerConfig
        {
            EntryCost            = s.EntryCost,
            DiceSides            = s.DiceSides,
            AutoWinCount         = s.AutoWinCount,
            TargetRounds         = s.TargetRounds,
            AllowMultipleWinners = s.AllowMultipleWinners,
            TradesToPotPercent   = s.TradesToPotPercent,
            Chat                 = DeepClone(s.Chat),
        };
    }

    private void ApplyBar777Settings(Bar777Config b)
    {
        this.config.Bar777.CustomName    = b.CustomName;
        this.config.Bar777.CostPerRoll   = b.CostPerRoll;
        this.config.Bar777.MaxRolls      = b.MaxRolls;
        this.config.Bar777.WinNumber     = b.WinNumber;
        this.config.Bar777.UseQueue      = b.UseQueue;
        this.config.Bar777.AutoCatchRoll = b.AutoCatchRoll;
        this.config.Bar777.Chat          = DeepClone(b.Chat);
    }

    private void ApplyDeathrollSettings(DeathrollTournamentConfig d)
    {
        this.config.DeathrollTournament.EntryCost                 = d.EntryCost;
        this.config.DeathrollTournament.BestOfPerRound            = new List<int>(d.BestOfPerRound);
        this.config.DeathrollTournament.AutoNextMatch             = d.AutoNextMatch;
        this.config.DeathrollTournament.AutoNextMatchDelaySeconds = d.AutoNextMatchDelaySeconds;
        this.config.DeathrollTournament.AutoCatchNextRound        = d.AutoCatchNextRound;
        this.config.DeathrollTournament.AutoJoinKeyword           = d.AutoJoinKeyword;
        this.config.DeathrollTournament.JoinKeyword               = d.JoinKeyword;
        this.config.DeathrollTournament.JoinChannels              = DeepClone(d.JoinChannels);
        this.config.DeathrollTournament.Chat                      = DeepClone(d.Chat);
        this.config.DeathrollTournament.WebhookUsername           = d.WebhookUsername;
        this.config.DeathrollTournament.WebhookAvatarUrl          = d.WebhookAvatarUrl;
        this.config.DeathrollTournament.DiscordWebhooks           = d.DiscordWebhooks.Select(e => new DeathrollTournamentDiscordEntry
        {
            Alias   = e.Alias,
            Url     = e.Url,
            Enabled = e.Enabled,
        }).ToList();
    }

    private void ApplyHigherLowerSettings(HigherLowerConfig h)
    {
        this.config.HigherLower.EntryCost            = h.EntryCost;
        this.config.HigherLower.DiceSides            = h.DiceSides;
        this.config.HigherLower.AutoWinCount         = h.AutoWinCount;
        this.config.HigherLower.TargetRounds         = h.TargetRounds;
        this.config.HigherLower.AllowMultipleWinners = h.AllowMultipleWinners;
        this.config.HigherLower.TradesToPotPercent   = h.TradesToPotPercent;
        this.config.HigherLower.Chat                 = DeepClone(h.Chat);
    }

    public string BuildExportString(bool bar777, bool deathroll, bool higherlower, bool discord, bool queue)
    {
        var payload = new PresetExportPayload
        {
            Bar777              = bar777      ? CaptureBar777Export()      : null,
            DeathrollTournament = deathroll   ? CaptureDeathrollExport()   : null,
            HigherLower         = higherlower ? CaptureHigherLowerExport() : null,
            Discord             = discord     ? CaptureDiscordExport()     : null,
            QueueKeyword        = queue       ? this.config.QueueKeyword   : null,
            QueueJoinChannels   = queue       ? DeepClone(this.config.QueueJoinChannels) : null,
        };
        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public int GetDefaultFallbackIndex() =>
        this.config.Presets.FindIndex(p => string.Equals(p.Name, "Default", StringComparison.OrdinalIgnoreCase));

    public bool TryParseExportString(string base64, out PresetExportPayload? payload)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64.Trim()));
            payload  = JsonSerializer.Deserialize<PresetExportPayload>(json);
            return payload != null;
        }
        catch
        {
            payload = null;
            return false;
        }
    }

    public bool TryImportPreset(string name, string base64, int? fallbackIndex, out string? error)
    {
        try
        {
            var json    = Encoding.UTF8.GetString(Convert.FromBase64String(base64.Trim()));
            var payload = JsonSerializer.Deserialize<PresetExportPayload>(json);
            if (payload == null) { error = "Invalid preset string."; return false; }
            var def = fallbackIndex.HasValue && fallbackIndex.Value >= 0 && fallbackIndex.Value < this.config.Presets.Count
                ? this.config.Presets[fallbackIndex.Value]
                : null;
            var preset = BuildPresetFromPayload(name, payload, def);
            this.config.Presets.Add(preset);
            this.config.ActivePresetIndex = this.config.Presets.Count - 1;
            ApplyPreset(this.config.ActivePresetIndex);
            error = null;
            return true;
        }
        catch (Exception)
        {
            error = "Could not decode the preset string. Please verify it is valid.";
            return false;
        }
    }

    private PluginPreset BuildPresetFromPayload(string name, PresetExportPayload payload, PluginPreset? def) => new()
    {
        Name                = name,
        Bar777              = BuildBar777FromPayload(payload.Bar777, def),
        DeathrollTournament = BuildDeathrollFromPayload(payload.DeathrollTournament, payload.Discord, def),
        HigherLower         = BuildHigherLowerFromPayload(payload.HigherLower, def),
        QueueKeyword        = payload.QueueKeyword        ?? def?.QueueKeyword        ?? "!join",
        QueueJoinChannels   = payload.QueueJoinChannels  != null
            ? DeepClone(payload.QueueJoinChannels)
            : (def != null ? DeepClone(def.QueueJoinChannels) : new QueueConfig()),
    };

    private static Bar777Config BuildBar777FromPayload(Bar777ExportEntry? entry, PluginPreset? def)
    {
        if (entry == null)
            return def != null ? DeepClone(def.Bar777) : new Bar777Config();
        return new Bar777Config
        {
            CustomName    = entry.CustomName,
            CostPerRoll   = entry.CostPerRoll,
            MaxRolls      = entry.MaxRolls,
            WinNumber     = entry.WinNumber,
            UseQueue      = entry.UseQueue,
            AutoCatchRoll = entry.AutoCatchRoll,
            Chat          = DeepClone(entry.Chat),
        };
    }

    private static HigherLowerConfig BuildHigherLowerFromPayload(HigherLowerExportEntry? entry, PluginPreset? def)
    {
        if (entry == null)
            return def != null ? DeepClone(def.HigherLower) : new HigherLowerConfig();
        return new HigherLowerConfig
        {
            EntryCost            = entry.EntryCost,
            DiceSides            = entry.DiceSides,
            AutoWinCount         = entry.AutoWinCount,
            TargetRounds         = entry.TargetRounds,
            AllowMultipleWinners = entry.AllowMultipleWinners,
            TradesToPotPercent   = entry.TradesToPotPercent,
            Chat                 = DeepClone(entry.Chat),
        };
    }

    private static DeathrollTournamentConfig BuildDeathrollFromPayload(
        DeathrollExportEntry? entry, DiscordExportEntry? discordEntry, PluginPreset? def)
    {
        var (webhooks, username, avatarUrl) = ResolveDiscord(discordEntry, def);
        if (entry == null)
        {
            var source = def != null ? DeepClone(def.DeathrollTournament) : new DeathrollTournamentConfig();
            source.DiscordWebhooks  = webhooks;
            source.WebhookUsername  = username;
            source.WebhookAvatarUrl = avatarUrl;
            return source;
        }
        return new DeathrollTournamentConfig
        {
            EntryCost                 = entry.EntryCost,
            BestOfPerRound            = new List<int>(entry.BestOfPerRound),
            AutoNextMatch             = entry.AutoNextMatch,
            AutoNextMatchDelaySeconds = entry.AutoNextMatchDelaySeconds,
            AutoCatchNextRound        = entry.AutoCatchNextRound,
            AutoJoinKeyword           = entry.AutoJoinKeyword,
            JoinKeyword               = entry.JoinKeyword,
            JoinChannels              = DeepClone(entry.JoinChannels),
            Chat                      = DeepClone(entry.Chat),
            WebhookUsername           = username,
            WebhookAvatarUrl          = avatarUrl,
            DiscordWebhooks           = webhooks,
        };
    }

    private static (List<DeathrollTournamentDiscordEntry> Webhooks, string Username, string AvatarUrl) ResolveDiscord(
        DiscordExportEntry? entry, PluginPreset? def)
    {
        if (entry != null)
            return (
                entry.Webhooks.Select(w => new DeathrollTournamentDiscordEntry
                {
                    Alias   = w.Alias,
                    Url     = w.Url,
                    Enabled = w.Enabled,
                }).ToList(),
                entry.WebhookUsername,
                entry.WebhookAvatarUrl
            );
        if (def != null)
            return (
                def.DeathrollTournament.DiscordWebhooks.Select(e => new DeathrollTournamentDiscordEntry
                {
                    Alias   = e.Alias,
                    Url     = e.Url,
                    Enabled = e.Enabled,
                }).ToList(),
                def.DeathrollTournament.WebhookUsername,
                def.DeathrollTournament.WebhookAvatarUrl
            );
        return (new List<DeathrollTournamentDiscordEntry>(), "Deathroll Tournament",
            "https://raw.githubusercontent.com/OOFGamesss/OOFGamesPlugins/main/images/deathrolltournament.png");
    }

    private Bar777ExportEntry CaptureBar777Export()
    {
        var s = this.config.Bar777;
        return new Bar777ExportEntry
        {
            CustomName    = s.CustomName,
            CostPerRoll   = s.CostPerRoll,
            MaxRolls      = s.MaxRolls,
            WinNumber     = s.WinNumber,
            UseQueue      = s.UseQueue,
            AutoCatchRoll = s.AutoCatchRoll,
            Chat          = DeepClone(s.Chat),
        };
    }

    private HigherLowerExportEntry CaptureHigherLowerExport()
    {
        var s = this.config.HigherLower;
        return new HigherLowerExportEntry
        {
            EntryCost            = s.EntryCost,
            DiceSides            = s.DiceSides,
            AutoWinCount         = s.AutoWinCount,
            TargetRounds         = s.TargetRounds,
            AllowMultipleWinners = s.AllowMultipleWinners,
            TradesToPotPercent   = s.TradesToPotPercent,
            Chat                 = DeepClone(s.Chat),
        };
    }

    private DeathrollExportEntry CaptureDeathrollExport()
    {
        var s = this.config.DeathrollTournament;
        return new DeathrollExportEntry
        {
            EntryCost                 = s.EntryCost,
            BestOfPerRound            = new List<int>(s.BestOfPerRound),
            AutoNextMatch             = s.AutoNextMatch,
            AutoNextMatchDelaySeconds = s.AutoNextMatchDelaySeconds,
            AutoCatchNextRound        = s.AutoCatchNextRound,
            AutoJoinKeyword           = s.AutoJoinKeyword,
            JoinKeyword               = s.JoinKeyword,
            JoinChannels              = DeepClone(s.JoinChannels),
            Chat                      = DeepClone(s.Chat),
        };
    }

    private DiscordExportEntry CaptureDiscordExport() => new()
    {
        Webhooks = this.config.DeathrollTournament.DiscordWebhooks.Select(e => new DiscordWebhookExportItem
        {
            Alias   = e.Alias,
            Url     = e.Url,
            Enabled = e.Enabled,
        }).ToList(),
        WebhookUsername  = this.config.DeathrollTournament.WebhookUsername,
        WebhookAvatarUrl = this.config.DeathrollTournament.WebhookAvatarUrl,
    };

    private static T DeepClone<T>(T obj) where T : notnull
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json)!;
    }
}
