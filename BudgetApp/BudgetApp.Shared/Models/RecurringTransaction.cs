using SQLite;

namespace BudgetApp.Shared.Models;

public enum RecurrenceFrequency
{
    Weekly,
    Biweekly,
    Monthly,
    Yearly
}

public class RecurringTransaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    
    public string Description { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    
    public int CategoryId { get; set; }
    
    public TransactionType Type { get; set; } = TransactionType.Expense;
    
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;
    
    /// <summary>
    /// The day of the month (1-31) when the transaction occurs.
    /// For weekly/biweekly, this is the start date's day.
    /// </summary>
    public int DayOfMonth { get; set; } = 1;
    
    /// <summary>
    /// The date when this recurring transaction was created/started.
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Today;
    
    /// <summary>
    /// The next date when a transaction should be generated.
    /// </summary>
    public DateTime NextDueDate { get; set; } = DateTime.Today;
    
    /// <summary>
    /// Whether this recurring transaction is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

