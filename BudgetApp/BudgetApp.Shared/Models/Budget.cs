using SQLite;

namespace BudgetApp.Shared.Models;

public class Budget
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

