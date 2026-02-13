namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Server-side Transaction entity. Mirrors BudgetApp.Shared.Models.Transaction.
/// CategoryId here references the server-side Category.Id (not the client's local ID).
/// During sync, the SyncService maps client CategorySyncIds to server CategoryIds.
/// </summary>
public class Transaction
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public int CategoryId { get; set; }
    public TransactionType Type { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
