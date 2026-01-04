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
        _database.CreateTable<RecurringTransaction>();

        // Seed default categories if empty
        if (_database.Table<Category>().Count() == 0)
        {
            SeedDefaultData();
        }
    }

    private void SeedDefaultData()
    {
        // Fixed categories - predictable, recurring expenses
        var fixedCategories = new List<Category>
        {
            new() { Name = "Rent/Mortgage", Icon = "🏠", Color = "#EF4444", DefaultBudget = 1500, Type = CategoryType.Fixed },
            new() { Name = "Bills & Utilities", Icon = "📄", Color = "#F97316", DefaultBudget = 300, Type = CategoryType.Fixed },
            new() { Name = "Insurance", Icon = "🛡️", Color = "#3B82F6", DefaultBudget = 200, Type = CategoryType.Fixed },
            new() { Name = "Subscriptions", Icon = "📱", Color = "#8B5CF6", DefaultBudget = 50, Type = CategoryType.Fixed },
            new() { Name = "Transport", Icon = "🚗", Color = "#06B6D4", DefaultBudget = 200, Type = CategoryType.Fixed }
        };

        // Discretionary categories - variable, optional spending
        var discretionaryCategories = new List<Category>
        {
            new() { Name = "Food & Dining", Icon = "🍽️", Color = "#F59E0B", DefaultBudget = 500, Type = CategoryType.Discretionary },
            new() { Name = "Shopping", Icon = "🛍️", Color = "#EC4899", DefaultBudget = 300, Type = CategoryType.Discretionary },
            new() { Name = "Entertainment", Icon = "🎬", Color = "#A855F7", DefaultBudget = 150, Type = CategoryType.Discretionary },
            new() { Name = "Health & Fitness", Icon = "💪", Color = "#10B981", DefaultBudget = 100, Type = CategoryType.Discretionary },
            new() { Name = "Personal Care", Icon = "💊", Color = "#14B8A6", DefaultBudget = 75, Type = CategoryType.Discretionary }
        };

        foreach (var category in fixedCategories)
        {
            _database!.Insert(category);
        }
        foreach (var category in discretionaryCategories)
        {
            _database!.Insert(category);
        }
    }

    // Categories
    public List<Category> GetCategories()
    {
        return Database.Table<Category>().ToList();
    }

    public Category? GetCategory(int categoryId)
    {
        return Database.Find<Category>(categoryId);
    }

    public void AddCategory(Category category)
    {
        Database.Insert(category);
    }

    public void UpdateCategory(Category category)
    {
        Database.Update(category);
    }

    public void DeleteCategory(int categoryId)
    {
        Database.Delete<Category>(categoryId);
    }

    public bool HasTransactionsForCategory(int categoryId)
    {
        return Database.Table<Transaction>().ToList().Any(t => t.CategoryId == categoryId);
    }

    public bool HasRecurringTransactionsForCategory(int categoryId)
    {
        return Database.Table<RecurringTransaction>().ToList().Any(r => r.CategoryId == categoryId);
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

    public void DeleteBudget(int categoryId, int month, int year)
    {
        var budget = GetBudget(categoryId, month, year);
        if (budget != null)
        {
            Database.Delete<Budget>(budget.Id);
        }
    }

    // Recurring Transactions
    public List<RecurringTransaction> GetRecurringTransactions()
    {
        return Database.Table<RecurringTransaction>().ToList();
    }

    public RecurringTransaction? GetRecurringTransaction(int id)
    {
        return Database.Find<RecurringTransaction>(id);
    }

    public void AddRecurringTransaction(RecurringTransaction recurring)
    {
        Database.Insert(recurring);
    }

    public void UpdateRecurringTransaction(RecurringTransaction recurring)
    {
        Database.Update(recurring);
    }

    public void DeleteRecurringTransaction(int id)
    {
        Database.Delete<RecurringTransaction>(id);
    }
}

