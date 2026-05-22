/// <summary>Value type carrying the raw bytes, filename, and slot index of a single file part in a multipart Discord API request.</summary>

namespace MiniGamesEmporium.Discord;

internal readonly record struct DiscordMultipartFilePart(byte[] Data, string Filename, int Slot);
