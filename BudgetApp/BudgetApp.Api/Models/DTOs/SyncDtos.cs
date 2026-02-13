namespace BudgetApp.Api.Models.DTOs;

/// <summary>
/// DTOs for the sync endpoint. These are simplified "flat" versions of the
/// server entities -- no navigation properties, no server-internal fields like
/// the integer Id or UserId. The client only sees SyncId-based records.
///
/// Each DTO contains exactly the fields the client sends/receives.
/// The SyncService maps between these DTOs and the EF Core entities.
/// </summary>

// ── Sync Request/Response ───────────────────────────────────────

public class SyncRequest
{
    /// <summary>
    /// When the client last synced. Server returns changes newer than this.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }

    /// <summary>
    /// All client-side changes since LastSyncedAt.
    /// </summary>
    public SyncPayloadDto ClientChanges { get; set; } = new();
}

public class SyncResponse
{
    /// <summary>
    /// Server-side changes the client needs to apply locally.
    /// </summary>
    public SyncPayloadDto ServerChanges { get; set; } = new();

    /// <summary>
    /// New sync timestamp. Client stores this for the next sync.
    /// </summary>
    public DateTime SyncedAt { get; set; }
}

public class SyncPayloadDto
{
    public List<CategoryDto> Categories { get; set; } = new();
    public List<TransactionDto> Transactions { get; set; } = new();
    public List<BudgetDto> Budgets { get; set; } = new();
    public List<RecurringTransactionDto> RecurringTransactions { get; set; } = new();
    public UserSettingsDto? Settings { get; set; }
    public List<SinkingFundDto> SinkingFunds { get; set; } = new();
    public List<SinkingFundTransactionDto> SinkingFundTransactions { get; set; } = new();
}

// ── Entity DTOs ─────────────────────────────────────────────────
// Each DTO mirrors the client model's syncable fields.
// No server-internal Id or UserId -- just SyncId + data + timestamps.

public class CategoryDto
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal DefaultBudget { get; set; }
    public int Type { get; set; } // CategoryType enum as int
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class TransactionDto
{
    public Guid SyncId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Guid CategorySyncId { get; set; } // References Category by SyncId, not int Id
    public int Type { get; set; } // TransactionType enum as int
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class BudgetDto
{
    public Guid SyncId { get; set; }
    public Guid CategorySyncId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class RecurringTransactionDto
{
    public Guid SyncId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid CategorySyncId { get; set; }
    public int Type { get; set; }
    public int Frequency { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class UserSettingsDto
{
    public Guid SyncId { get; set; }
    public decimal MonthlyIncome { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class SinkingFundDto
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal GoalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal MonthlyContribution { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public int Status { get; set; }
    public bool AutoContribute { get; set; }
    public DateTime? LastAutoContributeDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class SinkingFundTransactionDto
{
    public Guid SyncId { get; set; }
    public Guid SinkingFundSyncId { get; set; } // References SinkingFund by SyncId
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int Type { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
