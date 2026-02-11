using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;
using SQLite;

namespace BudgetApp.Shared.Services;

/// <summary>
/// Pure data-access layer backed by SQLite.
/// Contains no business logic — only CRUD and simple queries.
/// </summary>
public class DatabaseService : IDatabaseService
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
        _database.CreateTable<UserSettings>();
        _database.CreateTable<SinkingFund>();
        _database.CreateTable<SinkingFundTransaction>();

        // Seed default categories if empty
        if (_database.Table<Category>().Count() == 0)
        {
            SeedDefaultData();
        }

        // Ensure settings row exists
        if (_database.Table<UserSettings>().Count() == 0)
        {
            _database.Insert(new UserSettings { Id = 1, MonthlyIncome = 0 });
        }
    }

    private void SeedDefaultData()
    {
        var fixedCategories = new List<Category>
        {
            new() { Name = "Rent/Mortgage", Icon = "🏠", Color = "#EF4444", DefaultBudget = 1500, Type = CategoryType.Fixed },
            new() { Name = "Bills & Utilities", Icon = "📄", Color = "#F97316", DefaultBudget = 300, Type = CategoryType.Fixed },
            new() { Name = "Insurance", Icon = "🛡️", Color = "#3B82F6", DefaultBudget = 200, Type = CategoryType.Fixed },
            new() { Name = "Subscriptions", Icon = "📱", Color = "#8B5CF6", DefaultBudget = 50, Type = CategoryType.Fixed },
            new() { Name = "Transport", Icon = "🚗", Color = "#06B6D4", DefaultBudget = 200, Type = CategoryType.Fixed }
        };

        var discretionaryCategories = new List<Category>
        {
            new() { Name = "Food & Dining", Icon = "🍽️", Color = "#F59E0B", DefaultBudget = 500, Type = CategoryType.Discretionary },
            new() { Name = "Shopping", Icon = "🛍️", Color = "#EC4899", DefaultBudget = 300, Type = CategoryType.Discretionary },
            new() { Name = "Entertainment", Icon = "🎬", Color = "#A855F7", DefaultBudget = 150, Type = CategoryType.Discretionary },
            new() { Name = "Health & Fitness", Icon = "💪", Color = "#10B981", DefaultBudget = 100, Type = CategoryType.Discretionary },
            new() { Name = "Personal Care", Icon = "💊", Color = "#14B8A6", DefaultBudget = 75, Type = CategoryType.Discretionary }
        };

        foreach (var category in fixedCategories)
            _database!.Insert(category);
        foreach (var category in discretionaryCategories)
            _database!.Insert(category);
    }

    // ── Categories ──────────────────────────────────────────────

    public List<Category> GetCategories()
        => Database.Table<Category>().ToList();

    public Category? GetCategory(int categoryId)
        => Database.Find<Category>(categoryId);

    public void AddCategory(Category category)
        => Database.Insert(category);

    public void UpdateCategory(Category category)
        => Database.Update(category);

    public void DeleteCategory(int categoryId)
        => Database.Delete<Category>(categoryId);

    public bool HasTransactionsForCategory(int categoryId)
        => Database.Table<Transaction>().ToList().Any(t => t.CategoryId == categoryId);

    public bool HasRecurringTransactionsForCategory(int categoryId)
        => Database.Table<RecurringTransaction>().ToList().Any(r => r.CategoryId == categoryId);

    // ── Transactions ────────────────────────────────────────────

    public List<Transaction> GetTransactions(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return Database.Table<Transaction>()
            .ToList()
            .Where(t => t.Date >= startDate && t.Date < endDate)
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    public void AddTransaction(Transaction transaction)
        => Database.Insert(transaction);

    public void UpdateTransaction(Transaction transaction)
        => Database.Update(transaction);

    public void DeleteTransaction(int transactionId)
        => Database.Delete<Transaction>(transactionId);

    public Transaction? GetTransaction(int transactionId)
        => Database.Find<Transaction>(transactionId);

    // ── Budgets ─────────────────────────────────────────────────

    public List<Budget> GetBudgets(int month, int year)
        => Database.Table<Budget>()
            .ToList()
            .Where(b => b.Month == month && b.Year == year)
            .ToList();

    public Budget? GetBudget(int categoryId, int month, int year)
        => Database.Table<Budget>()
            .ToList()
            .FirstOrDefault(b => b.CategoryId == categoryId && b.Month == month && b.Year == year);

    public void UpsertBudget(int categoryId, int month, int year, decimal amount)
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
            Database.Delete<Budget>(budget.Id);
    }

    // ── Recurring Transactions ──────────────────────────────────

    public List<RecurringTransaction> GetRecurringTransactions()
        => Database.Table<RecurringTransaction>().ToList();

    public RecurringTransaction? GetRecurringTransaction(int id)
        => Database.Find<RecurringTransaction>(id);

    public void AddRecurringTransaction(RecurringTransaction recurring)
        => Database.Insert(recurring);

    public void UpdateRecurringTransaction(RecurringTransaction recurring)
        => Database.Update(recurring);

    public void DeleteRecurringTransaction(int id)
        => Database.Delete<RecurringTransaction>(id);

    // ── User Settings ───────────────────────────────────────────

    public UserSettings GetSettings()
        => Database.Find<UserSettings>(1) ?? new UserSettings { Id = 1, MonthlyIncome = 0 };

    public void UpdateSettings(UserSettings settings)
    {
        settings.Id = 1;
        Database.InsertOrReplace(settings);
    }

    // ── Sinking Funds ───────────────────────────────────────────

    public List<SinkingFund> GetSinkingFunds()
        => Database.Table<SinkingFund>().ToList();

    public SinkingFund? GetSinkingFund(int id)
        => Database.Find<SinkingFund>(id);

    public void AddSinkingFund(SinkingFund fund)
        => Database.Insert(fund);

    public void UpdateSinkingFund(SinkingFund fund)
        => Database.Update(fund);

    public void DeleteSinkingFund(int id)
    {
        // Delete all associated transactions first
        var transactions = GetSinkingFundTransactions(id);
        foreach (var tx in transactions)
            Database.Delete<SinkingFundTransaction>(tx.Id);
        Database.Delete<SinkingFund>(id);
    }

    // ── Sinking Fund Transactions ───────────────────────────────

    public List<SinkingFundTransaction> GetSinkingFundTransactions(int fundId)
        => Database.Table<SinkingFundTransaction>()
            .ToList()
            .Where(t => t.SinkingFundId == fundId)
            .OrderByDescending(t => t.Date)
            .ToList();

    public SinkingFundTransaction? GetSinkingFundTransaction(int transactionId)
        => Database.Find<SinkingFundTransaction>(transactionId);

    public void AddSinkingFundTransaction(SinkingFundTransaction transaction)
        => Database.Insert(transaction);

    public void DeleteSinkingFundTransaction(int transactionId)
        => Database.Delete<SinkingFundTransaction>(transactionId);

    public decimal GetTotalSinkingFundContributions(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return Database.Table<SinkingFundTransaction>()
            .ToList()
            .Where(t => t.Date >= startDate && t.Date < endDate
                     && t.Type == SinkingFundTransactionType.Contribution)
            .Sum(t => t.Amount);
    }
}
