using SQLite;

namespace BudgetApp.Shared.Models;

public class UserSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1; // Single row table
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public decimal MonthlyIncome { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Set to true once the user finishes (or explicitly skips) the onboarding
    /// wizard.  Replaces the old "income == 0" heuristic so users who skip or
    /// have zero income are not trapped in an onboarding loop.
    /// </summary>
    public bool HasCompletedOnboarding { get; set; }

    /// <summary>
    /// Persists the last successful sync timestamp so it survives app restarts.
    /// Without this, every app launch would reset to DateTime.MinValue and
    /// force a full data exchange with the server.
    /// This field is local-only -- it is NOT synced to the server (the server
    /// has its own UpdatedAt tracking).
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.MinValue;
}

