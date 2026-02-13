using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;
using SQLite;

namespace BudgetApp.Shared.Services;

/// <summary>
/// Pure data-access layer backed by SQLite.
/// Contains no business logic — only CRUD and simple queries.
///
/// SYNC SUPPORT (Phase 2):
/// - All "get" methods filter out soft-deleted records (IsDeleted == true)
/// - All inserts auto-set SyncId (if empty) and UpdatedAt
/// - All updates auto-set UpdatedAt
/// - All deletes are soft-deletes (set IsDeleted = true + bump UpdatedAt)
/// - Sync-specific methods provide unfiltered access for the sync service
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

        // Migrate existing records that don't have SyncIds yet.
        // This runs once after upgrading from a pre-sync version of the app.
        MigrateExistingSyncIds();
    }

    /// <summary>
    /// Assigns SyncId and UpdatedAt to any existing records that were created
    /// before sync columns were added. Without this, those records would have
    /// Guid.Empty as their SyncId and would be invisible to the sync service.
    /// </summary>
    private void MigrateExistingSyncIds()
    {
        MigrateSyncIds(_database!.Table<Category>().ToList(),
            (c, id, ts) => { c.SyncId = id; c.UpdatedAt = ts; _database.Update(c); });
        MigrateSyncIds(_database!.Table<Transaction>().ToList(),
            (t, id, ts) => { t.SyncId = id; t.UpdatedAt = ts; _database.Update(t); });
        MigrateSyncIds(_database!.Table<Budget>().ToList(),
            (b, id, ts) => { b.SyncId = id; b.UpdatedAt = ts; _database.Update(b); });
        MigrateSyncIds(_database!.Table<RecurringTransaction>().ToList(),
            (r, id, ts) => { r.SyncId = id; r.UpdatedAt = ts; _database.Update(r); });
        MigrateSyncIds(_database!.Table<SinkingFund>().ToList(),
            (f, id, ts) => { f.SyncId = id; f.UpdatedAt = ts; _database.Update(f); });
        MigrateSyncIds(_database!.Table<SinkingFundTransaction>().ToList(),
            (t, id, ts) => { t.SyncId = id; t.UpdatedAt = ts; _database.Update(t); });

        // UserSettings is a single row
        var settings = _database!.Find<UserSettings>(1);
        if (settings != null && settings.SyncId == Guid.Empty)
        {
            settings.SyncId = Guid.NewGuid();
            settings.UpdatedAt = DateTime.UtcNow;
            _database.Update(settings);
        }
    }

    /// <summary>
    /// Generic helper: for each record whose SyncId is Guid.Empty (meaning it
    /// was created before sync support), assign a new SyncId and set UpdatedAt.
    /// The applyFn callback handles the actual property assignment and database update,
    /// since each entity type has its own SyncId/UpdatedAt properties.
    /// </summary>
    private static void MigrateSyncIds<T>(List<T> records, Action<T, Guid, DateTime> applyFn)
    {
        var now = DateTime.UtcNow;
        foreach (var record in records)
        {
            // Check SyncId via reflection since we can't use generics with
            // property access directly (no shared base class).
            var syncIdProp = typeof(T).GetProperty("SyncId");
            if (syncIdProp != null)
            {
                var value = (Guid)syncIdProp.GetValue(record)!;
                if (value == Guid.Empty)
                {
                    applyFn(record, Guid.NewGuid(), now);
                }
            }
        }
    }

    private void SeedDefaultData()
    {
        // Stable SyncIds ensure default categories from different devices
        // represent the same logical records during sync.
        var fixedCategories = new List<Category>
        {
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0011"), Name = "Rent/Mortgage", Icon = "🏠", Color = "#EF4444", DefaultBudget = 1500, Type = CategoryType.Fixed },
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0012"), Name = "Bills & Utilities", Icon = "📄", Color = "#F97316", DefaultBudget = 300, Type = CategoryType.Fixed },
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0013"), Name = "Insurance", Icon = "🛡️", Color = "#3B82F6", DefaultBudget = 200, Type = CategoryType.Fixed },
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0014"), Name = "Subscriptions", Icon = "📱", Color = "#8B5CF6", DefaultBudget = 50, Type = CategoryType.Fixed },
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0015"), Name = "Transport", Icon = "🚗", Color = "#06B6D4", DefaultBudget = 200, Type = CategoryType.Fixed }
        };

        var discretionaryCategories = new List<Category>
        {
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0021"), Name = "Food & Dining", Icon = "🍽️", Color = "#F59E0B", DefaultBudget = 500, Type = CategoryType.Discretionary },
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0022"), Name = "Shopping", Icon = "🛍️", Color = "#EC4899", DefaultBudget = 300, Type = CategoryType.Discretionary },
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0023"), Name = "Entertainment", Icon = "🎬", Color = "#A855F7", DefaultBudget = 150, Type = CategoryType.Discretionary },
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0024"), Name = "Health & Fitness", Icon = "💪", Color = "#10B981", DefaultBudget = 100, Type = CategoryType.Discretionary },
            new() { SyncId = Guid.Parse("5ef209e2-67f7-4ce8-95b6-a5a1f3bc0025"), Name = "Personal Care", Icon = "💊", Color = "#14B8A6", DefaultBudget = 75, Type = CategoryType.Discretionary }
        };

        foreach (var category in fixedCategories)
            _database!.Insert(category);
        foreach (var category in discretionaryCategories)
            _database!.Insert(category);
    }

    /// <summary>
    /// Helper: stamps UpdatedAt to now. Called before every insert and update.
    /// </summary>
    private static DateTime StampNow() => DateTime.UtcNow;

    // ── Categories ──────────────────────────────────────────────

    public List<Category> GetCategories()
        => Database.Table<Category>().ToList().Where(c => !c.IsDeleted).ToList();

    public Category? GetCategory(int categoryId)
    {
        var cat = Database.Find<Category>(categoryId);
        return cat is { IsDeleted: false } ? cat : null;
    }

    public void AddCategory(Category category)
    {
        if (category.SyncId == Guid.Empty) category.SyncId = Guid.NewGuid();
        category.UpdatedAt = StampNow();
        Database.Insert(category);
    }

    public void UpdateCategory(Category category)
    {
        category.UpdatedAt = StampNow();
        Database.Update(category);
    }

    public void DeleteCategory(int categoryId)
    {
        var cat = Database.Find<Category>(categoryId);
        if (cat != null)
        {
            cat.IsDeleted = true;
            cat.UpdatedAt = StampNow();
            Database.Update(cat);
        }
    }

    public bool HasTransactionsForCategory(int categoryId)
        => Database.Table<Transaction>().ToList()
            .Any(t => t.CategoryId == categoryId && !t.IsDeleted);

    public bool HasRecurringTransactionsForCategory(int categoryId)
        => Database.Table<RecurringTransaction>().ToList()
            .Any(r => r.CategoryId == categoryId && !r.IsDeleted);

    // ── Transactions ────────────────────────────────────────────

    public List<Transaction> GetTransactions(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return Database.Table<Transaction>()
            .ToList()
            .Where(t => !t.IsDeleted && t.Date >= startDate && t.Date < endDate)
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    public List<Transaction> SearchTransactions(string query, int? categoryId = null, int maxResults = 100)
    {
        var all = Database.Table<Transaction>()
            .ToList()
            .Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            all = all.Where(t => t.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            all = all.Where(t => t.CategoryId == categoryId.Value);
        }
        else if (categoryId.HasValue && categoryId.Value == -1)
        {
            // -1 = income filter
            all = all.Where(t => t.Type == TransactionType.Income);
        }

        return all
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(maxResults)
            .ToList();
    }

    public void AddTransaction(Transaction transaction)
    {
        if (transaction.SyncId == Guid.Empty) transaction.SyncId = Guid.NewGuid();
        transaction.UpdatedAt = StampNow();
        Database.Insert(transaction);
    }

    public void UpdateTransaction(Transaction transaction)
    {
        transaction.UpdatedAt = StampNow();
        Database.Update(transaction);
    }

    public void DeleteTransaction(int transactionId)
    {
        var txn = Database.Find<Transaction>(transactionId);
        if (txn != null)
        {
            txn.IsDeleted = true;
            txn.UpdatedAt = StampNow();
            Database.Update(txn);
        }
    }

    public Transaction? GetTransaction(int transactionId)
    {
        var txn = Database.Find<Transaction>(transactionId);
        return txn is { IsDeleted: false } ? txn : null;
    }

    // ── Budgets ─────────────────────────────────────────────────

    public List<Budget> GetBudgets(int month, int year)
        => Database.Table<Budget>()
            .ToList()
            .Where(b => !b.IsDeleted && b.Month == month && b.Year == year)
            .ToList();

    public Budget? GetBudget(int categoryId, int month, int year)
        => Database.Table<Budget>()
            .ToList()
            .FirstOrDefault(b => !b.IsDeleted && b.CategoryId == categoryId
                              && b.Month == month && b.Year == year);

    public void UpsertBudget(int categoryId, int month, int year, decimal amount)
    {
        var budget = GetBudget(categoryId, month, year);
        if (budget != null)
        {
            budget.Amount = amount;
            budget.UpdatedAt = StampNow();
            Database.Update(budget);
        }
        else
        {
            Database.Insert(new Budget
            {
                CategoryId = categoryId,
                Month = month,
                Year = year,
                Amount = amount,
                SyncId = Guid.NewGuid(),
                UpdatedAt = StampNow()
            });
        }
    }

    public void DeleteBudget(int categoryId, int month, int year)
    {
        var budget = GetBudget(categoryId, month, year);
        if (budget != null)
        {
            budget.IsDeleted = true;
            budget.UpdatedAt = StampNow();
            Database.Update(budget);
        }
    }

    // ── Recurring Transactions ──────────────────────────────────

    public List<RecurringTransaction> GetRecurringTransactions()
        => Database.Table<RecurringTransaction>().ToList().Where(r => !r.IsDeleted).ToList();

    public RecurringTransaction? GetRecurringTransaction(int id)
    {
        var r = Database.Find<RecurringTransaction>(id);
        return r is { IsDeleted: false } ? r : null;
    }

    public void AddRecurringTransaction(RecurringTransaction recurring)
    {
        if (recurring.SyncId == Guid.Empty) recurring.SyncId = Guid.NewGuid();
        recurring.UpdatedAt = StampNow();
        Database.Insert(recurring);
    }

    public void UpdateRecurringTransaction(RecurringTransaction recurring)
    {
        recurring.UpdatedAt = StampNow();
        Database.Update(recurring);
    }

    public void DeleteRecurringTransaction(int id)
    {
        var r = Database.Find<RecurringTransaction>(id);
        if (r != null)
        {
            r.IsDeleted = true;
            r.UpdatedAt = StampNow();
            Database.Update(r);
        }
    }

    // ── User Settings ───────────────────────────────────────────

    public UserSettings GetSettings()
        => Database.Find<UserSettings>(1) ?? new UserSettings { Id = 1, MonthlyIncome = 0 };

    public void UpdateSettings(UserSettings settings)
    {
        settings.Id = 1;
        settings.UpdatedAt = StampNow();
        Database.InsertOrReplace(settings);
    }

    // ── Sinking Funds ───────────────────────────────────────────

    public List<SinkingFund> GetSinkingFunds()
        => Database.Table<SinkingFund>().ToList().Where(f => !f.IsDeleted).ToList();

    public SinkingFund? GetSinkingFund(int id)
    {
        var f = Database.Find<SinkingFund>(id);
        return f is { IsDeleted: false } ? f : null;
    }

    public void AddSinkingFund(SinkingFund fund)
    {
        if (fund.SyncId == Guid.Empty) fund.SyncId = Guid.NewGuid();
        fund.UpdatedAt = StampNow();
        Database.Insert(fund);
    }

    public void UpdateSinkingFund(SinkingFund fund)
    {
        fund.UpdatedAt = StampNow();
        Database.Update(fund);
    }

    public void DeleteSinkingFund(int id)
    {
        // Soft-delete all associated transactions first
        var transactions = Database.Table<SinkingFundTransaction>().ToList()
            .Where(t => t.SinkingFundId == id && !t.IsDeleted).ToList();
        var now = StampNow();
        foreach (var tx in transactions)
        {
            tx.IsDeleted = true;
            tx.UpdatedAt = now;
            Database.Update(tx);
        }

        var fund = Database.Find<SinkingFund>(id);
        if (fund != null)
        {
            fund.IsDeleted = true;
            fund.UpdatedAt = now;
            Database.Update(fund);
        }
    }

    // ── Sinking Fund Transactions ───────────────────────────────

    public List<SinkingFundTransaction> GetSinkingFundTransactions(int fundId)
        => Database.Table<SinkingFundTransaction>()
            .ToList()
            .Where(t => !t.IsDeleted && t.SinkingFundId == fundId)
            .OrderByDescending(t => t.Date)
            .ToList();

    public SinkingFundTransaction? GetSinkingFundTransaction(int transactionId)
    {
        var t = Database.Find<SinkingFundTransaction>(transactionId);
        return t is { IsDeleted: false } ? t : null;
    }

    public void AddSinkingFundTransaction(SinkingFundTransaction transaction)
    {
        if (transaction.SyncId == Guid.Empty) transaction.SyncId = Guid.NewGuid();
        transaction.UpdatedAt = StampNow();
        Database.Insert(transaction);
    }

    public void DeleteSinkingFundTransaction(int transactionId)
    {
        var t = Database.Find<SinkingFundTransaction>(transactionId);
        if (t != null)
        {
            t.IsDeleted = true;
            t.UpdatedAt = StampNow();
            Database.Update(t);
        }
    }

    public decimal GetTotalSinkingFundContributions(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        return Database.Table<SinkingFundTransaction>()
            .ToList()
            .Where(t => !t.IsDeleted && t.Date >= startDate && t.Date < endDate
                     && t.Type == SinkingFundTransactionType.Contribution)
            .Sum(t => t.Amount);
    }

    // ── Persisted Sync Timestamp ─────────────────────────────────
    // Stored in the UserSettings row so it survives app restarts.

    public DateTime GetLastSyncedAt()
    {
        var settings = Database.Find<UserSettings>(1);
        return settings?.LastSyncedAt ?? DateTime.MinValue;
    }

    public void SetLastSyncedAt(DateTime value)
    {
        var settings = Database.Find<UserSettings>(1);
        if (settings != null)
        {
            settings.LastSyncedAt = value;
            // NOTE: we intentionally do NOT bump UpdatedAt here.
            // LastSyncedAt is a local-only field and should not trigger
            // the record to be re-synced to the server.
            Database.Update(settings);
        }
    }

    // ── Sync Methods ────────────────────────────────────────────
    // These are used by ISyncService to gather local changes and apply
    // server changes. They include soft-deleted records because the sync
    // service needs to propagate deletions.

    public SyncPayload GetChangesSince(DateTime since)
    {
        return new SyncPayload
        {
            LastSyncedAt = since,
            Categories = Database.Table<Category>().ToList()
                .Where(c => c.UpdatedAt > since).ToList(),
            Transactions = Database.Table<Transaction>().ToList()
                .Where(t => t.UpdatedAt > since).ToList(),
            Budgets = Database.Table<Budget>().ToList()
                .Where(b => b.UpdatedAt > since).ToList(),
            RecurringTransactions = Database.Table<RecurringTransaction>().ToList()
                .Where(r => r.UpdatedAt > since).ToList(),
            Settings = Database.Find<UserSettings>(1) is { } s && s.UpdatedAt > since ? s : null,
            SinkingFunds = Database.Table<SinkingFund>().ToList()
                .Where(f => f.UpdatedAt > since).ToList(),
            SinkingFundTransactions = Database.Table<SinkingFundTransaction>().ToList()
                .Where(t => t.UpdatedAt > since).ToList()
        };
    }

    /// <summary>
    /// Collapses duplicate categories in the local SQLite database.
    ///
    /// Two passes:
    /// 1. Exact duplicates (same SyncId): keeps the row with the lowest Id,
    ///    hard-deletes the extras. These should never exist, but concurrent
    ///    sync races could have created them.
    /// 2. Semantic duplicates (same Name+Type but different SyncIds): keeps
    ///    the most recently updated row, repoints child FK references
    ///    (Transactions, Budgets, RecurringTransactions), and soft-deletes
    ///    the extras so the server can propagate the deletion.
    ///
    /// Called at the start of ApplyServerChanges so the database is clean
    /// before any new data is applied.
    /// </summary>
    public void NormalizeLocalCategories()
    {
        var allCats = Database.Table<Category>().ToList();
        if (allCats.Count < 2) return;

        var now = StampNow();

        // ── Pass 1: collapse exact SyncId duplicates ────────────────
        var syncIdGroups = allCats
            .GroupBy(c => c.SyncId)
            .Where(g => g.Count() > 1);

        foreach (var group in syncIdGroups)
        {
            var keep = group.OrderBy(c => c.Id).First();
            foreach (var dup in group.Where(c => c.Id != keep.Id))
            {
                RepointCategoryChildren(dup.Id, keep.Id, now);
                Database.Delete(dup); // hard-delete (exact duplicate)
            }
        }

        // ── Pass 2: collapse semantic (name+type) duplicates ────────
        var remaining = Database.Table<Category>().ToList()
            .Where(c => !c.IsDeleted).ToList();

        static string Norm(string s) => (s ?? string.Empty).Trim().ToLowerInvariant();

        var nameGroups = remaining
            .GroupBy(c => new { Name = Norm(c.Name), c.Type })
            .Where(g => g.Count() > 1);

        foreach (var group in nameGroups)
        {
            var keep = group
                .OrderByDescending(c => c.UpdatedAt)
                .ThenBy(c => c.Id)
                .First();

            foreach (var dup in group.Where(c => c.Id != keep.Id))
            {
                RepointCategoryChildren(dup.Id, keep.Id, now);
                dup.IsDeleted = true;
                dup.UpdatedAt = now;
                Database.Update(dup);
            }
        }
    }

    /// <summary>
    /// Repoints all child entities that reference oldCategoryId to newCategoryId.
    /// </summary>
    private void RepointCategoryChildren(int oldCategoryId, int newCategoryId, DateTime now)
    {
        foreach (var t in Database.Table<Transaction>().ToList()
            .Where(t => t.CategoryId == oldCategoryId))
        {
            t.CategoryId = newCategoryId;
            t.UpdatedAt = now;
            Database.Update(t);
        }
        foreach (var b in Database.Table<Budget>().ToList()
            .Where(b => b.CategoryId == oldCategoryId))
        {
            b.CategoryId = newCategoryId;
            b.UpdatedAt = now;
            Database.Update(b);
        }
        foreach (var r in Database.Table<RecurringTransaction>().ToList()
            .Where(r => r.CategoryId == oldCategoryId))
        {
            r.CategoryId = newCategoryId;
            r.UpdatedAt = now;
            Database.Update(r);
        }
    }

    /// <summary>
    /// Applies changes received from the server to the local SQLite database.
    /// Uses SyncId to match records. Last-write-wins: if the server record's
    /// UpdatedAt is newer, it overwrites the local version.
    ///
    /// This is the core of the sync algorithm on the client side.
    /// </summary>
    public void ApplyServerChanges(SyncPayload changes)
    {
        // Clean up any local duplicates before applying new data.
        NormalizeLocalCategories();
        foreach (var serverCat in changes.Categories)
            UpsertBySyncId(serverCat);

        foreach (var serverTxn in changes.Transactions)
            UpsertBySyncId(serverTxn);

        foreach (var serverBudget in changes.Budgets)
            UpsertBySyncId(serverBudget);

        foreach (var serverRecurring in changes.RecurringTransactions)
            UpsertBySyncId(serverRecurring);

        if (changes.Settings != null)
            UpsertSettingsFromServer(changes.Settings);

        foreach (var serverFund in changes.SinkingFunds)
            UpsertBySyncId(serverFund);

        foreach (var serverFundTxn in changes.SinkingFundTransactions)
            UpsertBySyncId(serverFundTxn);
    }

    // ── Sync upsert helpers ─────────────────────────────────────
    // Each helper finds a local record by SyncId. If it exists and the server
    // version is newer, it overwrites. If it doesn't exist, it inserts.

    private void UpsertBySyncId(Category server)
    {
        var local = Database.Table<Category>().ToList()
            .FirstOrDefault(c => c.SyncId == server.SyncId);
        if (local != null)
        {
            if (server.UpdatedAt > local.UpdatedAt)
            {
                server.Id = local.Id; // Keep the local auto-increment Id
                Database.Update(server);
            }
        }
        else
        {
            server.Id = 0; // Let SQLite auto-increment assign a new Id
            Database.Insert(server);
        }
    }

    private void UpsertBySyncId(Transaction server)
    {
        var local = Database.Table<Transaction>().ToList()
            .FirstOrDefault(t => t.SyncId == server.SyncId);
        if (local != null)
        {
            if (server.UpdatedAt > local.UpdatedAt)
            {
                server.Id = local.Id;
                Database.Update(server);
            }
        }
        else
        {
            server.Id = 0;
            Database.Insert(server);
        }
    }

    private void UpsertBySyncId(Budget server)
    {
        var local = Database.Table<Budget>().ToList()
            .FirstOrDefault(b => b.SyncId == server.SyncId);
        if (local != null)
        {
            if (server.UpdatedAt > local.UpdatedAt)
            {
                server.Id = local.Id;
                Database.Update(server);
            }
        }
        else
        {
            server.Id = 0;
            Database.Insert(server);
        }
    }

    private void UpsertBySyncId(RecurringTransaction server)
    {
        var local = Database.Table<RecurringTransaction>().ToList()
            .FirstOrDefault(r => r.SyncId == server.SyncId);
        if (local != null)
        {
            if (server.UpdatedAt > local.UpdatedAt)
            {
                server.Id = local.Id;
                Database.Update(server);
            }
        }
        else
        {
            server.Id = 0;
            Database.Insert(server);
        }
    }

    private void UpsertSettingsFromServer(UserSettings server)
    {
        var local = Database.Find<UserSettings>(1);
        if (local != null)
        {
            if (server.UpdatedAt > local.UpdatedAt)
            {
                server.Id = 1; // Always Id=1 for settings
                Database.Update(server);
            }
        }
        else
        {
            server.Id = 1;
            Database.InsertOrReplace(server);
        }
    }

    private void UpsertBySyncId(SinkingFund server)
    {
        var local = Database.Table<SinkingFund>().ToList()
            .FirstOrDefault(f => f.SyncId == server.SyncId);
        if (local != null)
        {
            if (server.UpdatedAt > local.UpdatedAt)
            {
                server.Id = local.Id;
                Database.Update(server);
            }
        }
        else
        {
            server.Id = 0;
            Database.Insert(server);
        }
    }

    private void UpsertBySyncId(SinkingFundTransaction server)
    {
        var local = Database.Table<SinkingFundTransaction>().ToList()
            .FirstOrDefault(t => t.SyncId == server.SyncId);
        if (local != null)
        {
            if (server.UpdatedAt > local.UpdatedAt)
            {
                server.Id = local.Id;
                Database.Update(server);
            }
        }
        else
        {
            server.Id = 0;
            Database.Insert(server);
        }
    }
}
