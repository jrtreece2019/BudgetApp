namespace BudgetApp.Shared.Models;

public enum TransactionType
{
    Expense,
    Income
}

public class Transaction
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int CategoryId { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
}

