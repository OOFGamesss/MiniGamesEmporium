using Dalamud.Plugin;
using LiteDB;
using MiniGamesEmporium.Config;
using MiniGamesEmporium.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>Manages persistent session and transaction history in LiteDB with in-memory caches.</summary>

namespace MiniGamesEmporium.Services;
public sealed class HistoryService : IDisposable
{
    private readonly LiteDatabase db;
    private readonly ILiteCollection<SessionRecord> sessions;
    private readonly ILiteCollection<TransactionRecord> transactions;
    private List<SessionRecord> cachedSessions = new();
    private List<TransactionRecord> cachedTransactions = new();

    public IReadOnlyList<SessionRecord> CachedSessions => this.cachedSessions;
    public IReadOnlyList<TransactionRecord> CachedTransactions => this.cachedTransactions;

    public HistoryService(IDalamudPluginInterface pluginInterface, PluginConfiguration config)
    {
        var mapper = new BsonMapper();
        mapper.Entity<SessionRecord>().Id(s => s.SessionId);
        mapper.Entity<TransactionRecord>().Id(t => t.TransactionId);
        var dbPath = Path.Combine(pluginInterface.ConfigDirectory.FullName, "history.db");
        this.db = new LiteDatabase(dbPath, mapper);
        this.sessions = this.db.GetCollection<SessionRecord>("sessions");
        this.transactions = this.db.GetCollection<TransactionRecord>("transactions");
        MigrateIfNeeded(config);
        RefreshCaches();
    }

    private void MigrateIfNeeded(PluginConfiguration config)
    {
        if (config.HasMigratedHistoryToLiteDb) return;
        if (config.SessionHistory.Count > 0)
        {
            foreach (var record in config.SessionHistory)
                if (record.SessionId == Guid.Empty) record.SessionId = Guid.NewGuid();
            this.sessions.InsertBulk(config.SessionHistory);
        }
        if (config.Transactions.Count > 0)
        {
            foreach (var record in config.Transactions)
                if (record.TransactionId == Guid.Empty) record.TransactionId = Guid.NewGuid();
            this.transactions.InsertBulk(config.Transactions);
        }
        config.SessionHistory.Clear();
        config.Transactions.Clear();
        config.HasMigratedHistoryToLiteDb = true;
        config.Save();
    }

    private void RefreshCaches()
    {
        this.cachedSessions = this.sessions.FindAll().OrderBy(s => s.Timestamp).ToList();
        this.cachedTransactions = this.transactions.FindAll().OrderBy(t => t.Timestamp).ToList();
    }

    public void AddSession(SessionRecord record)
    {
        this.sessions.Insert(record);
        RefreshCaches();
    }

    public void AddTransaction(TransactionRecord record)
    {
        this.transactions.Insert(record);
        RefreshCaches();
    }

    public void DeleteSession(Guid id)
    {
        this.sessions.Delete(new BsonValue(id));
        RefreshCaches();
    }

    public void ClearSessions()
    {
        this.sessions.DeleteAll();
        RefreshCaches();
    }

    public void Dispose()
    {
        this.db.Dispose();
    }
}
