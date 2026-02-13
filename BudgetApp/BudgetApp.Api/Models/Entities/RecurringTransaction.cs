namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Server-side RecurringTransaction entity. Stores templates for transactions
/// that repeat on a schedule (e.g., rent, Netflix subscription).
/// The client-side RecurrenceCalculator processes these locally; the server
/// just stores the definition for sync purposes.
/// </summary>
public class RecurringTransaction
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int CategoryId { get; set; }
    public TransactionType Type { get; set; }
    public RecurrenceFrequency Frequency { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
