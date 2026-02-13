namespace BudgetApp.Shared.Models;

/// <summary>
/// The data exchanged between client and server during a sync operation.
/// Used in both directions:
///   - Client → Server: "here are my local changes since last sync"
///   - Server → Client: "here are the server changes since your last sync"
///
/// Each list contains records that were created, updated, OR soft-deleted
/// since the given timestamp. The receiving side uses SyncId to match
/// records and UpdatedAt for last-write-wins conflict resolution.
/// </summary>
public class SyncPayload
{
    /// <summary>
    /// The point in time these changes are relative to.
    /// Client sends: "changes since this time". Server responds: "changes since this time".
    /// </summary>
    public DateTime LastSyncedAt { get; set; }

    public List<Category> Categories { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
    public List<Budget> Budgets { get; set; } = new();
    public List<RecurringTransaction> RecurringTransactions { get; set; } = new();
    public UserSettings? Settings { get; set; }
    public List<SinkingFund> SinkingFunds { get; set; } = new();
    public List<SinkingFundTransaction> SinkingFundTransactions { get; set; } = new();
}

/// <summary>
/// The request body sent by the client to POST /api/sync.
/// Contains the client's local changes and the timestamp of the last successful sync.
/// </summary>
public class SyncRequest
{
    /// <summary>
    /// When the client last successfully synced. The server uses this to find
    /// server-side changes the client hasn't seen yet.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }

    /// <summary>
    /// All records the client has created, updated, or deleted since LastSyncedAt.
    /// </summary>
    public SyncPayload ClientChanges { get; set; } = new();
}

/// <summary>
/// The response body returned by POST /api/sync.
/// Contains server-side changes the client needs to apply locally.
/// </summary>
public class SyncResponse
{
    /// <summary>
    /// All records that changed on the server since the client's LastSyncedAt.
    /// The client applies these to its local SQLite database.
    /// </summary>
    public SyncPayload ServerChanges { get; set; } = new();

    /// <summary>
    /// The new "last synced" timestamp. The client stores this and sends it
    /// on the next sync so the server only returns newer changes.
    /// </summary>
    public DateTime SyncedAt { get; set; }
}
