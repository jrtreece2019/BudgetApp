using SQLite;

namespace BudgetApp.Shared.Models;

public enum SinkingFundTransactionType
{
    Contribution = 0,
    Withdrawal = 1
}

public class SinkingFundTransaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    
    [Indexed]
    public int SinkingFundId { get; set; }
    
    public DateTime Date { get; set; } = DateTime.Today;
    
    public decimal Amount { get; set; }
    
    public SinkingFundTransactionType Type { get; set; } = SinkingFundTransactionType.Contribution;
    
    public string Note { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

