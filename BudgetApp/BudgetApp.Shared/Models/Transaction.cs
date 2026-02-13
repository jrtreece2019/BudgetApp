using SQLite;

namespace BudgetApp.Shared.Models;

public enum TransactionType
{
    Expense,
    Income
}

public class Transaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int CategoryId { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

