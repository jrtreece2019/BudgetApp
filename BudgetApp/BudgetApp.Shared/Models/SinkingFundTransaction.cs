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
    
    [Indexed]
    public int SinkingFundId { get; set; }
    
    public DateTime Date { get; set; } = DateTime.Today;
    
    public decimal Amount { get; set; }
    
    public SinkingFundTransactionType Type { get; set; } = SinkingFundTransactionType.Contribution;
    
    public string Note { get; set; } = string.Empty;
}

