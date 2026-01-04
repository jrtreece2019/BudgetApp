using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services;

public class SqliteBudgetService : IBudgetService
{
    private readonly DatabaseService _db;

    public SqliteBudgetService(DatabaseService db)
    {
        _db = db;
    }

    public List<Category> GetCategories()
    {
        return _db.GetCategories();
    }

    public List<Transaction> GetTransactions(int month, int year)
    {
        var transactions = _db.GetTransactions(month, year);
        return transactions.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToList();
    }

    public List<Budget> GetBudgets(int month, int year)
    {
        return _db.GetBudgets(month, year);
    }

    public decimal GetTotalBudget(int month, int year)
    {
        return _db.GetBudgets(month, year).Sum(b => b.Amount);
    }

    public decimal GetTotalSpent(int month, int year)
    {
        return _db.GetTransactions(month, year)
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
    }

    public decimal GetSpentByCategory(int categoryId, int month, int year)
    {
        return _db.GetTransactions(month, year)
            .Where(t => t.CategoryId == categoryId && t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
    }

    public decimal GetBudgetByCategory(int categoryId, int month, int year)
    {
        var budget = _db.GetBudget(categoryId, month, year);
        return budget?.Amount ?? 0;
    }

    public void AddTransaction(Transaction transaction)
    {
        _db.AddTransaction(transaction);
    }

    public void UpdateTransaction(Transaction transaction)
    {
        _db.UpdateTransaction(transaction);
    }

    public void DeleteTransaction(int transactionId)
    {
        _db.DeleteTransaction(transactionId);
    }

    public Transaction? GetTransaction(int transactionId)
    {
        return _db.GetTransaction(transactionId);
    }

    public void UpdateBudget(int categoryId, int month, int year, decimal amount)
    {
        _db.UpdateBudget(categoryId, month, year, amount);
    }
}

