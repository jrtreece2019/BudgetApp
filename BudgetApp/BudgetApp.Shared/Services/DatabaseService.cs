using BudgetApp.Shared.Models;
using SQLite;

namespace BudgetApp.Shared.Services;

public class DatabaseService
{
    private SQLiteConnection? _database;
    private readonly string _dbPath;

    public DatabaseService(string dbPath)
    {
        _dbPath = dbPath;
    }

    private SQLiteConnection Database
    {
        get
        {
            if (_database == null)
            {
                _database = new SQLiteConnection(_dbPath);
                InitializeDatabase();
            }
            return _database;
        }
    }

    private void InitializeDatabase()
    {
        _database!.CreateTable<Category>();
        _database.CreateTable<Transaction>();
        _database.CreateTable<Budget>();

        // Seed default categories if empty
        if (_database.Table<Category>().Count() == 0)
        {
            SeedDefaultData();
        }
    }

    private void SeedDefaultData()
    {
        var categories = new List<Category>
        {
            new() { Name = "Food & Dining", Icon = "🍽️", Color = "#F59E0B" },
            new() { Name = "Transport", Icon = "🚗", Color = "#3B82F6" },
            new() { Name = "Bills & Utilities", Icon = "📄", Color = "#EF4444" },
            new() { Name = "Shopping", Icon = "🛍️", Color = "#EC4899" },
            new() { Name = "Entertainment", Icon = "🎬", Color = "#8B5CF6" },
            new() { Name = "Health", Icon = "💊", Color = "#10B981" }
        };

        foreach (var category in categories)
        {
            _database!.Insert(category);
        }

        // Seed default budgets for current month
        var now = DateTime.Now;
        var defaultBudgets = new List<Budget>
        {
            new() { CategoryId = 1, Amount = 500, Month = now.Month, Year = now.Year },
            new() { CategoryId = 2, Amount = 200, Month = now.Month, Year = now.Year },
            new() { CategoryId = 3, Amount = 800, Month = now.Month, Year = now.Year },
            new() { CategoryId = 4, Amount = 300, Month = now.Month, Year = now.Year },
            new() { CategoryId = 5, Amount = 150, Month = now.Month, Year = now.Year },
            new() { CategoryId = 6, Amount = 100, Month = now.Month, Year = now.Year }
        };

        foreach (var budget in defaultBudgets)
        {
            _database!.Insert(budget);
        }
    }

    // Categories
    public List<Category> GetCategories()
    {
        return Database.Table<Category>().ToList();
    }

    // Transactions
    public List<Transaction> GetTransactions(int month, int year)
    {
        // Filter in memory since SQLite-net doesn't support DateTime.Month/Year in queries
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);
        
        return Database.Table<Transaction>()
            .ToList()
            .Where(t => t.Date >= startDate && t.Date < endDate)
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    public void AddTransaction(Transaction transaction)
    {
        Database.Insert(transaction);
    }

    public void UpdateTransaction(Transaction transaction)
    {
        Database.Update(transaction);
    }

    public void DeleteTransaction(int transactionId)
    {
        Database.Delete<Transaction>(transactionId);
    }

    public Transaction? GetTransaction(int transactionId)
    {
        return Database.Find<Transaction>(transactionId);
    }

    // Budgets
    public List<Budget> GetBudgets(int month, int year)
    {
        // Filter in memory since SQLite-net has limitations with complex queries
        return Database.Table<Budget>()
            .ToList()
            .Where(b => b.Month == month && b.Year == year)
            .ToList();
    }

    public Budget? GetBudget(int categoryId, int month, int year)
    {
        // Filter in memory
        return Database.Table<Budget>()
            .ToList()
            .FirstOrDefault(b => b.CategoryId == categoryId && b.Month == month && b.Year == year);
    }

    public void UpdateBudget(int categoryId, int month, int year, decimal amount)
    {
        var budget = GetBudget(categoryId, month, year);
        if (budget != null)
        {
            budget.Amount = amount;
            Database.Update(budget);
        }
        else
        {
            Database.Insert(new Budget
            {
                CategoryId = categoryId,
                Month = month,
                Year = year,
                Amount = amount
            });
        }
    }
}

