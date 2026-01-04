using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services;

public class BudgetService : IBudgetService
{
    private readonly List<Category> _categories;
    private readonly List<Transaction> _transactions;
    private readonly List<Budget> _budgets;
    private int _nextTransactionId = 100;

    public BudgetService()
    {
        _categories = new List<Category>
        {
            new() { Id = 1, Name = "Food & Dining", Icon = "🍽️", Color = "#F59E0B" },
            new() { Id = 2, Name = "Transport", Icon = "🚗", Color = "#3B82F6" },
            new() { Id = 3, Name = "Bills & Utilities", Icon = "📄", Color = "#EF4444" },
            new() { Id = 4, Name = "Shopping", Icon = "🛍️", Color = "#EC4899" },
            new() { Id = 5, Name = "Entertainment", Icon = "🎬", Color = "#8B5CF6" },
            new() { Id = 6, Name = "Health", Icon = "💊", Color = "#10B981" }
        };

        var now = DateTime.Now;
        
        _budgets = new List<Budget>
        {
            new() { Id = 1, CategoryId = 1, Amount = 500, Month = now.Month, Year = now.Year },
            new() { Id = 2, CategoryId = 2, Amount = 200, Month = now.Month, Year = now.Year },
            new() { Id = 3, CategoryId = 3, Amount = 800, Month = now.Month, Year = now.Year },
            new() { Id = 4, CategoryId = 4, Amount = 300, Month = now.Month, Year = now.Year },
            new() { Id = 5, CategoryId = 5, Amount = 150, Month = now.Month, Year = now.Year },
            new() { Id = 6, CategoryId = 6, Amount = 100, Month = now.Month, Year = now.Year }
        };

        _transactions = new List<Transaction>
        {
            new() { Id = 1, Description = "Grocery Store", Amount = 85.50m, Date = now.AddDays(-1), CategoryId = 1, Type = TransactionType.Expense },
            new() { Id = 2, Description = "Coffee Shop", Amount = 5.75m, Date = now, CategoryId = 1, Type = TransactionType.Expense },
            new() { Id = 3, Description = "Gas Station", Amount = 45.00m, Date = now.AddDays(-2), CategoryId = 2, Type = TransactionType.Expense },
            new() { Id = 4, Description = "Electric Bill", Amount = 120.00m, Date = now.AddDays(-5), CategoryId = 3, Type = TransactionType.Expense },
            new() { Id = 5, Description = "Internet", Amount = 79.99m, Date = now.AddDays(-5), CategoryId = 3, Type = TransactionType.Expense },
            new() { Id = 6, Description = "New Shoes", Amount = 89.99m, Date = now.AddDays(-3), CategoryId = 4, Type = TransactionType.Expense },
            new() { Id = 7, Description = "Netflix", Amount = 15.99m, Date = now.AddDays(-10), CategoryId = 5, Type = TransactionType.Expense },
            new() { Id = 8, Description = "Pharmacy", Amount = 25.00m, Date = now.AddDays(-4), CategoryId = 6, Type = TransactionType.Expense },
            new() { Id = 9, Description = "Paycheck", Amount = 2500.00m, Date = now.AddDays(-15), CategoryId = 1, Type = TransactionType.Income }
        };
    }

    public List<Category> GetCategories() => _categories;

    public List<Transaction> GetTransactions(int month, int year)
    {
        return _transactions
            .Where(t => t.Date.Month == month && t.Date.Year == year)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToList();
    }

    public List<Budget> GetBudgets(int month, int year)
    {
        return _budgets
            .Where(b => b.Month == month && b.Year == year)
            .ToList();
    }

    public decimal GetTotalBudget(int month, int year)
    {
        return _budgets
            .Where(b => b.Month == month && b.Year == year)
            .Sum(b => b.Amount);
    }

    public decimal GetTotalSpent(int month, int year)
    {
        return _transactions
            .Where(t => t.Date.Month == month && t.Date.Year == year && t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
    }

    public decimal GetSpentByCategory(int categoryId, int month, int year)
    {
        return _transactions
            .Where(t => t.CategoryId == categoryId && t.Date.Month == month && t.Date.Year == year && t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
    }

    public decimal GetBudgetByCategory(int categoryId, int month, int year)
    {
        return _budgets
            .Where(b => b.CategoryId == categoryId && b.Month == month && b.Year == year)
            .Sum(b => b.Amount);
    }

    public void AddTransaction(Transaction transaction)
    {
        transaction.Id = _nextTransactionId++;
        _transactions.Add(transaction);
    }

    public void UpdateTransaction(Transaction transaction)
    {
        var existing = _transactions.FirstOrDefault(t => t.Id == transaction.Id);
        if (existing != null)
        {
            existing.Description = transaction.Description;
            existing.Amount = transaction.Amount;
            existing.Date = transaction.Date;
            existing.CategoryId = transaction.CategoryId;
            existing.Type = transaction.Type;
        }
    }

    public void DeleteTransaction(int transactionId)
    {
        var transaction = _transactions.FirstOrDefault(t => t.Id == transactionId);
        if (transaction != null)
        {
            _transactions.Remove(transaction);
        }
    }

    public Transaction? GetTransaction(int transactionId)
    {
        return _transactions.FirstOrDefault(t => t.Id == transactionId);
    }

    public void UpdateBudget(int categoryId, int month, int year, decimal amount)
    {
        var budget = _budgets.FirstOrDefault(b => b.CategoryId == categoryId && b.Month == month && b.Year == year);
        if (budget != null)
        {
            budget.Amount = amount;
        }
        else
        {
            // Create new budget if it doesn't exist
            _budgets.Add(new Budget
            {
                Id = _budgets.Count + 1,
                CategoryId = categoryId,
                Month = month,
                Year = year,
                Amount = amount
            });
        }
    }
}

