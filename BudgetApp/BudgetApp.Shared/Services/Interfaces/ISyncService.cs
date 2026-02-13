namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Manages bidirectional data sync between the local SQLite database
/// and the remote BudgetApp.Api server.
///
/// HOW IT WORKS:
/// 1. The service remembers when it last synced (LastSyncedAt timestamp).
/// 2. On SyncAsync(), it asks DatabaseService for all records with
///    UpdatedAt > LastSyncedAt (the local changes).
/// 3. It sends those changes to POST /api/sync along with LastSyncedAt.
/// 4. The server applies the client changes, then returns any server-side
///    changes the client hasn't seen yet.
/// 5. The client applies the server changes to local SQLite.
/// 6. LastSyncedAt is updated to the server's SyncedAt timestamp.
///
/// WHEN IT RUNS:
/// - On app startup (after login, to pull changes from other devices)
/// - After any local write (push immediately)
/// - On a periodic timer (catch anything missed)
/// - On manual "pull to refresh"
///
/// If the user isn't logged in, sync is a no-op.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// True if a sync is currently in progress.
    /// </summary>
    bool IsSyncing { get; }

    /// <summary>
    /// When the last successful sync completed (UTC).
    /// DateTime.MinValue if never synced.
    /// </summary>
    DateTime LastSyncedAt { get; }

    /// <summary>
    /// Performs a full sync cycle: push local changes, pull server changes.
    /// Returns true if sync succeeded, false if it failed (e.g., offline).
    /// </summary>
    Task<bool> SyncAsync();

    /// <summary>
    /// Starts a background timer that syncs periodically.
    /// Call this once after the user logs in.
    /// </summary>
    void StartAutoSync(TimeSpan interval);

    /// <summary>
    /// Stops the background sync timer.
    /// Call this when the user logs out.
    /// </summary>
    void StopAutoSync();
}
