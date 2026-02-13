using BudgetApp.Api.Data;
using BudgetApp.Api.Models.DTOs;
using BudgetApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetApp.Api.Services;

/// <summary>
/// Server-side sync engine. Handles the two-way data exchange between
/// client devices and the PostgreSQL database.
///
/// ALGORITHM:
/// 1. Receive client changes (SyncPayloadDto with SyncId-based records)
/// 2. For each client record, find the server record by (UserId, SyncId)
///    - If not found → insert it
///    - If found and client UpdatedAt is newer → overwrite it (last-write-wins)
///    - If found and server UpdatedAt is newer → skip it (server wins)
/// 3. Query all server records where UpdatedAt > client's lastSyncedAt
/// 4. Return those as the server changes for the client to apply locally
///
/// IMPORTANT: Foreign keys (e.g., Transaction.CategoryId) are resolved
/// from SyncIds. The client sends CategorySyncId; the server looks up
/// the corresponding server-side Category by (UserId, SyncId) to get
/// the integer Id for the FK.
/// </summary>
public class SyncService
{
    private readonly AppDbContext _db;

    public SyncService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Ensures a DateTime has Kind=Utc so PostgreSQL accepts it.
    ///
    /// WHY THIS EXISTS:
    /// The client stores dates in SQLite, which has no concept of time zones.
    /// When those dates travel over JSON to the API, they arrive with
    /// Kind=Unspecified.  PostgreSQL's "timestamp with time zone" column
    /// requires Kind=Utc.  This helper marks Unspecified values as Utc
    /// (they already ARE Utc in practice, just not tagged that way).
    /// </summary>
    private static DateTime ToUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            : dt.ToUniversalTime();

    /// <summary>Nullable overload for optional date fields.</summary>
    private static DateTime? ToUtc(DateTime? dt) =>
        dt.HasValue ? ToUtc(dt.Value) : null;

    /// <summary>
    /// Checks the EF Core change tracker for a pending (Added) entity that
    /// matches the given predicate.  FirstOrDefaultAsync only queries the
    /// DATABASE; it does not see entities that have been Add()'d but not
    /// yet SaveChanges()'d.  When two concurrent API requests both process
    /// the same SyncId, both DB queries return null and both insert -- creating
    /// duplicates.  This helper closes that gap within a single request scope.
    /// </summary>
    private T? FindInChangeTracker<T>(Func<T, bool> predicate) where T : class =>
        _db.ChangeTracker.Entries<T>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified
                     || e.State == EntityState.Unchanged)
            .Select(e => e.Entity)
            .FirstOrDefault(predicate);

    /// <summary>
    /// Main sync method. Processes client changes, then returns server changes.
    /// </summary>
    public async Task<SyncResponse> ProcessSyncAsync(string userId, SyncRequest request)
    {
        var now = DateTime.UtcNow;

        // Step 1: Apply client changes to the server database.
        await ApplyClientChangesAsync(userId, request.ClientChanges);

        // Step 1.5: Collapse accidental category duplicates for this user.
        // This can happen when multiple devices seed "default categories"
        // independently with different SyncIds.
        await NormalizeDuplicateCategoriesAsync(userId);

        // Step 2: Gather server changes since the client's last sync.
        // ToUtc ensures the client's timestamp is properly tagged for PostgreSQL comparison.
        var serverChanges = await GetServerChangesSinceAsync(userId, ToUtc(request.LastSyncedAt));

        return new SyncResponse
        {
            ServerChanges = serverChanges,
            SyncedAt = now
        };
    }

    // ── Apply Client Changes ────────────────────────────────────

    private async Task ApplyClientChangesAsync(string userId, SyncPayloadDto changes)
    {
        // ── Phase 1: "Parent" entities ──────────────────────────────
        // Categories and SinkingFunds must be saved to the database FIRST
        // because other entities (Transactions, Budgets, RecurringTransactions,
        // SinkingFundTransactions) hold foreign keys that reference them.
        //
        // If we don't save here, the FK lookup queries (e.g., "find Category
        // by SyncId") will return null for newly-created categories, causing
        // CategoryId to resolve to 0 and triggering a FK constraint violation.
        foreach (var dto in changes.Categories)
            await UpsertCategoryAsync(userId, dto);

        foreach (var dto in changes.SinkingFunds)
            await UpsertSinkingFundAsync(userId, dto);

        // Flush parent entities so FK lookups in Phase 2 can find them.
        await _db.SaveChangesAsync();

        // ── Phase 2: "Child" entities that reference parents ────────
        foreach (var dto in changes.Transactions)
            await UpsertTransactionAsync(userId, dto);

        foreach (var dto in changes.Budgets)
            await UpsertBudgetAsync(userId, dto);

        foreach (var dto in changes.RecurringTransactions)
            await UpsertRecurringTransactionAsync(userId, dto);

        if (changes.Settings != null)
            await UpsertSettingsAsync(userId, changes.Settings);

        foreach (var dto in changes.SinkingFundTransactions)
            await UpsertSinkingFundTransactionAsync(userId, dto);

        await _db.SaveChangesAsync();
    }

    // ── Category ────────────────────────────────────────────────

    private async Task UpsertCategoryAsync(string userId, CategoryDto dto)
    {
        var existing = await _db.Categories
            .FirstOrDefaultAsync(c => c.UserId == userId && c.SyncId == dto.SyncId);

        // Also check the change tracker for entities Added but not yet saved.
        existing ??= FindInChangeTracker<Category>(c => c.UserId == userId && c.SyncId == dto.SyncId);

        if (existing == null)
        {
            _db.Categories.Add(new Category
            {
                SyncId = dto.SyncId,
                UserId = userId,
                Name = dto.Name,
                Icon = dto.Icon,
                Color = dto.Color,
                DefaultBudget = dto.DefaultBudget,
                Type = (CategoryType)dto.Type,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = ToUtc(dto.UpdatedAt),
                IsDeleted = dto.IsDeleted
            });
        }
        else if (ToUtc(dto.UpdatedAt) > existing.UpdatedAt)
        {
            existing.Name = dto.Name;
            existing.Icon = dto.Icon;
            existing.Color = dto.Color;
            existing.DefaultBudget = dto.DefaultBudget;
            existing.Type = (CategoryType)dto.Type;
            existing.UpdatedAt = ToUtc(dto.UpdatedAt);
            existing.IsDeleted = dto.IsDeleted;
        }
    }

    // ── Transaction ─────────────────────────────────────────────

    private async Task UpsertTransactionAsync(string userId, TransactionDto dto)
    {
        // Resolve CategorySyncId → server CategoryId.
        // If the category doesn't exist, fall back to "Uncategorized" to avoid FK violations.
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.UserId == userId && c.SyncId == dto.CategorySyncId);
        var categoryId = category?.Id ?? await GetOrCreateUncategorizedIdAsync(userId);

        var existing = await _db.Transactions
            .FirstOrDefaultAsync(t => t.UserId == userId && t.SyncId == dto.SyncId);
        existing ??= FindInChangeTracker<Transaction>(t => t.UserId == userId && t.SyncId == dto.SyncId);

        if (existing == null)
        {
            _db.Transactions.Add(new Transaction
            {
                SyncId = dto.SyncId,
                UserId = userId,
                Description = dto.Description,
                Amount = dto.Amount,
                Date = ToUtc(dto.Date),
                CategoryId = categoryId,
                Type = (TransactionType)dto.Type,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = ToUtc(dto.UpdatedAt),
                IsDeleted = dto.IsDeleted
            });
        }
        else if (ToUtc(dto.UpdatedAt) > existing.UpdatedAt)
        {
            existing.Description = dto.Description;
            existing.Amount = dto.Amount;
            existing.Date = ToUtc(dto.Date);
            existing.CategoryId = categoryId;
            existing.Type = (TransactionType)dto.Type;
            existing.UpdatedAt = ToUtc(dto.UpdatedAt);
            existing.IsDeleted = dto.IsDeleted;
        }
    }

    // ── Budget ──────────────────────────────────────────────────

    private async Task UpsertBudgetAsync(string userId, BudgetDto dto)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.UserId == userId && c.SyncId == dto.CategorySyncId);
        var categoryId = category?.Id ?? await GetOrCreateUncategorizedIdAsync(userId);

        var existing = await _db.Budgets
            .FirstOrDefaultAsync(b => b.UserId == userId && b.SyncId == dto.SyncId);
        existing ??= FindInChangeTracker<Budget>(b => b.UserId == userId && b.SyncId == dto.SyncId);

        if (existing == null)
        {
            _db.Budgets.Add(new Budget
            {
                SyncId = dto.SyncId,
                UserId = userId,
                CategoryId = categoryId,
                Amount = dto.Amount,
                Month = dto.Month,
                Year = dto.Year,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = ToUtc(dto.UpdatedAt),
                IsDeleted = dto.IsDeleted
            });
        }
        else if (ToUtc(dto.UpdatedAt) > existing.UpdatedAt)
        {
            existing.CategoryId = categoryId;
            existing.Amount = dto.Amount;
            existing.Month = dto.Month;
            existing.Year = dto.Year;
            existing.UpdatedAt = ToUtc(dto.UpdatedAt);
            existing.IsDeleted = dto.IsDeleted;
        }
    }

    // ── RecurringTransaction ────────────────────────────────────

    private async Task UpsertRecurringTransactionAsync(string userId, RecurringTransactionDto dto)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.UserId == userId && c.SyncId == dto.CategorySyncId);
        var categoryId = category?.Id ?? await GetOrCreateUncategorizedIdAsync(userId);

        var existing = await _db.RecurringTransactions
            .FirstOrDefaultAsync(r => r.UserId == userId && r.SyncId == dto.SyncId);
        existing ??= FindInChangeTracker<RecurringTransaction>(r => r.UserId == userId && r.SyncId == dto.SyncId);

        if (existing == null)
        {
            _db.RecurringTransactions.Add(new RecurringTransaction
            {
                SyncId = dto.SyncId,
                UserId = userId,
                Description = dto.Description,
                Amount = dto.Amount,
                CategoryId = categoryId,
                Type = (TransactionType)dto.Type,
                Frequency = (RecurrenceFrequency)dto.Frequency,
                DayOfMonth = dto.DayOfMonth,
                StartDate = ToUtc(dto.StartDate),
                NextDueDate = ToUtc(dto.NextDueDate),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = ToUtc(dto.UpdatedAt),
                IsDeleted = dto.IsDeleted
            });
        }
        else if (ToUtc(dto.UpdatedAt) > existing.UpdatedAt)
        {
            existing.Description = dto.Description;
            existing.Amount = dto.Amount;
            existing.CategoryId = categoryId;
            existing.Type = (TransactionType)dto.Type;
            existing.Frequency = (RecurrenceFrequency)dto.Frequency;
            existing.DayOfMonth = dto.DayOfMonth;
            existing.StartDate = ToUtc(dto.StartDate);
            existing.NextDueDate = ToUtc(dto.NextDueDate);
            existing.IsActive = dto.IsActive;
            existing.UpdatedAt = ToUtc(dto.UpdatedAt);
            existing.IsDeleted = dto.IsDeleted;
        }
    }

    // ── UserSettings ────────────────────────────────────────────

    private async Task UpsertSettingsAsync(string userId, UserSettingsDto dto)
    {
        var existing = await _db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (existing == null)
        {
            _db.UserSettings.Add(new UserSettings
            {
                SyncId = dto.SyncId,
                UserId = userId,
                MonthlyIncome = dto.MonthlyIncome,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = ToUtc(dto.UpdatedAt),
                IsDeleted = dto.IsDeleted
            });
        }
        else if (ToUtc(dto.UpdatedAt) > existing.UpdatedAt)
        {
            existing.MonthlyIncome = dto.MonthlyIncome;
            existing.UpdatedAt = ToUtc(dto.UpdatedAt);
            existing.IsDeleted = dto.IsDeleted;
        }
    }

    // ── SinkingFund ─────────────────────────────────────────────

    private async Task UpsertSinkingFundAsync(string userId, SinkingFundDto dto)
    {
        var existing = await _db.SinkingFunds
            .FirstOrDefaultAsync(f => f.UserId == userId && f.SyncId == dto.SyncId);
        existing ??= FindInChangeTracker<SinkingFund>(f => f.UserId == userId && f.SyncId == dto.SyncId);

        if (existing == null)
        {
            _db.SinkingFunds.Add(new SinkingFund
            {
                SyncId = dto.SyncId,
                UserId = userId,
                Name = dto.Name,
                Icon = dto.Icon,
                Color = dto.Color,
                GoalAmount = dto.GoalAmount,
                CurrentBalance = dto.CurrentBalance,
                MonthlyContribution = dto.MonthlyContribution,
                StartDate = ToUtc(dto.StartDate),
                TargetDate = ToUtc(dto.TargetDate),
                Status = (SinkingFundStatus)dto.Status,
                AutoContribute = dto.AutoContribute,
                LastAutoContributeDate = ToUtc(dto.LastAutoContributeDate),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = ToUtc(dto.UpdatedAt),
                IsDeleted = dto.IsDeleted
            });
        }
        else if (ToUtc(dto.UpdatedAt) > existing.UpdatedAt)
        {
            existing.Name = dto.Name;
            existing.Icon = dto.Icon;
            existing.Color = dto.Color;
            existing.GoalAmount = dto.GoalAmount;
            existing.CurrentBalance = dto.CurrentBalance;
            existing.MonthlyContribution = dto.MonthlyContribution;
            existing.StartDate = ToUtc(dto.StartDate);
            existing.TargetDate = ToUtc(dto.TargetDate);
            existing.Status = (SinkingFundStatus)dto.Status;
            existing.AutoContribute = dto.AutoContribute;
            existing.LastAutoContributeDate = ToUtc(dto.LastAutoContributeDate);
            existing.UpdatedAt = ToUtc(dto.UpdatedAt);
            existing.IsDeleted = dto.IsDeleted;
        }
    }

    // ── SinkingFundTransaction ──────────────────────────────────

    private async Task UpsertSinkingFundTransactionAsync(string userId, SinkingFundTransactionDto dto)
    {
        var fund = await _db.SinkingFunds
            .FirstOrDefaultAsync(f => f.UserId == userId && f.SyncId == dto.SinkingFundSyncId);
        var fundId = fund?.Id ?? await GetOrCreateFallbackSinkingFundIdAsync(userId);

        var existing = await _db.SinkingFundTransactions
            .FirstOrDefaultAsync(t => t.UserId == userId && t.SyncId == dto.SyncId);
        existing ??= FindInChangeTracker<SinkingFundTransaction>(t => t.UserId == userId && t.SyncId == dto.SyncId);

        if (existing == null)
        {
            _db.SinkingFundTransactions.Add(new SinkingFundTransaction
            {
                SyncId = dto.SyncId,
                UserId = userId,
                SinkingFundId = fundId,
                Date = ToUtc(dto.Date),
                Amount = dto.Amount,
                Type = (SinkingFundTransactionType)dto.Type,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = ToUtc(dto.UpdatedAt),
                IsDeleted = dto.IsDeleted
            });
        }
        else if (ToUtc(dto.UpdatedAt) > existing.UpdatedAt)
        {
            existing.SinkingFundId = fundId;
            existing.Date = ToUtc(dto.Date);
            existing.Amount = dto.Amount;
            existing.Type = (SinkingFundTransactionType)dto.Type;
            existing.Note = dto.Note;
            existing.UpdatedAt = ToUtc(dto.UpdatedAt);
            existing.IsDeleted = dto.IsDeleted;
        }
    }

    // ── Get Server Changes ──────────────────────────────────────

    /// <summary>
    /// Returns all records for this user that have been modified since the given timestamp.
    /// Includes soft-deleted records so the client can propagate deletions.
    /// Maps server entities (with integer FKs) back to DTOs (with SyncId references).
    /// </summary>
    private async Task<SyncPayloadDto> GetServerChangesSinceAsync(string userId, DateTime since)
    {
        // Build a lookup of server CategoryId → SyncId for FK mapping.
        var categoryLookup = await _db.Categories
            .Where(c => c.UserId == userId)
            .ToDictionaryAsync(c => c.Id, c => c.SyncId);

        var fundLookup = await _db.SinkingFunds
            .Where(f => f.UserId == userId)
            .ToDictionaryAsync(f => f.Id, f => f.SyncId);

        var categories = await _db.Categories
            .Where(c => c.UserId == userId && c.UpdatedAt > since)
            .ToListAsync();

        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId && t.UpdatedAt > since)
            .ToListAsync();

        var budgets = await _db.Budgets
            .Where(b => b.UserId == userId && b.UpdatedAt > since)
            .ToListAsync();

        var recurringTransactions = await _db.RecurringTransactions
            .Where(r => r.UserId == userId && r.UpdatedAt > since)
            .ToListAsync();

        var settings = await _db.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.UpdatedAt > since);

        var sinkingFunds = await _db.SinkingFunds
            .Where(f => f.UserId == userId && f.UpdatedAt > since)
            .ToListAsync();

        var sinkingFundTransactions = await _db.SinkingFundTransactions
            .Where(t => t.UserId == userId && t.UpdatedAt > since)
            .ToListAsync();

        return new SyncPayloadDto
        {
            Categories = categories.Select(c => new CategoryDto
            {
                SyncId = c.SyncId,
                Name = c.Name,
                Icon = c.Icon,
                Color = c.Color,
                DefaultBudget = c.DefaultBudget,
                Type = (int)c.Type,
                UpdatedAt = c.UpdatedAt,
                IsDeleted = c.IsDeleted
            }).ToList(),

            Transactions = transactions.Select(t => new TransactionDto
            {
                SyncId = t.SyncId,
                Description = t.Description,
                Amount = t.Amount,
                Date = t.Date,
                CategorySyncId = categoryLookup.GetValueOrDefault(t.CategoryId),
                Type = (int)t.Type,
                UpdatedAt = t.UpdatedAt,
                IsDeleted = t.IsDeleted
            }).ToList(),

            Budgets = budgets.Select(b => new BudgetDto
            {
                SyncId = b.SyncId,
                CategorySyncId = categoryLookup.GetValueOrDefault(b.CategoryId),
                Amount = b.Amount,
                Month = b.Month,
                Year = b.Year,
                UpdatedAt = b.UpdatedAt,
                IsDeleted = b.IsDeleted
            }).ToList(),

            RecurringTransactions = recurringTransactions.Select(r => new RecurringTransactionDto
            {
                SyncId = r.SyncId,
                Description = r.Description,
                Amount = r.Amount,
                CategorySyncId = categoryLookup.GetValueOrDefault(r.CategoryId),
                Type = (int)r.Type,
                Frequency = (int)r.Frequency,
                DayOfMonth = r.DayOfMonth,
                StartDate = r.StartDate,
                NextDueDate = r.NextDueDate,
                IsActive = r.IsActive,
                UpdatedAt = r.UpdatedAt,
                IsDeleted = r.IsDeleted
            }).ToList(),

            Settings = settings == null ? null : new UserSettingsDto
            {
                SyncId = settings.SyncId,
                MonthlyIncome = settings.MonthlyIncome,
                UpdatedAt = settings.UpdatedAt,
                IsDeleted = settings.IsDeleted
            },

            SinkingFunds = sinkingFunds.Select(f => new SinkingFundDto
            {
                SyncId = f.SyncId,
                Name = f.Name,
                Icon = f.Icon,
                Color = f.Color,
                GoalAmount = f.GoalAmount,
                CurrentBalance = f.CurrentBalance,
                MonthlyContribution = f.MonthlyContribution,
                StartDate = f.StartDate,
                TargetDate = f.TargetDate,
                Status = (int)f.Status,
                AutoContribute = f.AutoContribute,
                LastAutoContributeDate = f.LastAutoContributeDate,
                UpdatedAt = f.UpdatedAt,
                IsDeleted = f.IsDeleted
            }).ToList(),

            SinkingFundTransactions = sinkingFundTransactions.Select(t => new SinkingFundTransactionDto
            {
                SyncId = t.SyncId,
                SinkingFundSyncId = fundLookup.GetValueOrDefault(t.SinkingFundId),
                Date = t.Date,
                Amount = t.Amount,
                Type = (int)t.Type,
                Note = t.Note,
                UpdatedAt = t.UpdatedAt,
                IsDeleted = t.IsDeleted
            }).ToList()
        };
    }

    // ── Fallback Category ────────────────────────────────────────

    /// <summary>
    /// Gets or creates an "Uncategorized" category for the given user.
    /// Used as a safe fallback when a CategorySyncId can't be resolved
    /// (e.g., the referenced category was deleted or hasn't synced yet).
    /// Without this, we'd get FK constraint violations from CategoryId = 0.
    /// </summary>
    private async Task<int> GetOrCreateUncategorizedIdAsync(string userId)
    {
        var fallback = await _db.Categories
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == "Uncategorized");

        if (fallback != null)
            return fallback.Id;

        fallback = new Category
        {
            SyncId = Guid.NewGuid(),
            UserId = userId,
            Name = "Uncategorized",
            Icon = "❓",
            Color = "#6B7280",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Categories.Add(fallback);
        await _db.SaveChangesAsync();
        return fallback.Id;
    }

    /// <summary>
    /// Gets or creates a fallback sinking fund for unresolved SinkingFundSyncId references.
    /// This prevents FK violations when a client sends a transaction that references
    /// a sinking fund that doesn't exist on the server yet (or was deleted).
    /// </summary>
    private async Task<int> GetOrCreateFallbackSinkingFundIdAsync(string userId)
    {
        var fallback = await _db.SinkingFunds
            .FirstOrDefaultAsync(f => f.UserId == userId && f.Name == "Unassigned Sinking Fund");

        if (fallback != null)
            return fallback.Id;

        fallback = new SinkingFund
        {
            SyncId = Guid.NewGuid(),
            UserId = userId,
            Name = "Unassigned Sinking Fund",
            Icon = "🏦",
            Color = "#6B7280",
            GoalAmount = 0m,
            CurrentBalance = 0m,
            MonthlyContribution = 0m,
            StartDate = DateTime.UtcNow,
            TargetDate = null,
            Status = SinkingFundStatus.Active,
            AutoContribute = false,
            LastAutoContributeDate = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _db.SinkingFunds.Add(fallback);
        await _db.SaveChangesAsync();
        return fallback.Id;
    }

    /// <summary>
    /// Merges duplicate categories for a user. Two passes:
    ///
    /// Pass 1 -- Same-SyncId duplicates (exact duplicates from race conditions):
    ///   Multiple rows with the same SyncId should never exist. They happen when
    ///   concurrent sync requests both insert before either calls SaveChanges.
    ///   We keep the row with the highest Id (most recently auto-generated) and
    ///   HARD-DELETE the extras -- they are not real data, just race artifacts.
    ///
    /// Pass 2 -- Same-Name+Type duplicates (semantic duplicates from multi-device seeding):
    ///   Different SyncIds but identical Name+Type (e.g., two "Rent/Mortgage" Fixed).
    ///   We keep the most recently updated row, repoint child FKs, and SOFT-DELETE
    ///   the extras so the deletion propagates to all clients via sync.
    /// </summary>
    private async Task NormalizeDuplicateCategoriesAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var changed = false;

        // ── Pass 1: collapse same-SyncId duplicates (hard-delete) ───
        var allCategories = await _db.Categories
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var syncIdGroups = allCategories
            .GroupBy(c => c.SyncId)
            .Where(g => g.Count() > 1);

        foreach (var group in syncIdGroups)
        {
            var keep = group.OrderByDescending(c => c.Id).First();
            var extras = group.Where(c => c.Id != keep.Id).ToList();
            foreach (var extra in extras)
            {
                await RepointCategoryChildrenAsync(userId, extra.Id, keep.Id, now);
                _db.Categories.Remove(extra); // hard-delete
                changed = true;
            }
        }

        if (changed)
            await _db.SaveChangesAsync();

        // ── Pass 2: collapse semantic (Name+Type) duplicates (soft-delete)
        var nonDeleted = await _db.Categories
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .ToListAsync();

        if (nonDeleted.Count < 2)
            return;

        static string NormalizeName(string value) => (value ?? string.Empty).Trim().ToLowerInvariant();
        var changed2 = false;

        var nameGroups = nonDeleted
            .GroupBy(c => new { Name = NormalizeName(c.Name), c.Type })
            .Where(g => g.Count() > 1);

        foreach (var group in nameGroups)
        {
            var keep = group
                .OrderByDescending(c => c.UpdatedAt)
                .ThenBy(c => c.Id)
                .First();

            var duplicates = group.Where(c => c.Id != keep.Id).ToList();
            foreach (var duplicate in duplicates)
            {
                await RepointCategoryChildrenAsync(userId, duplicate.Id, keep.Id, now);
                duplicate.IsDeleted = true;
                duplicate.UpdatedAt = now;
                changed2 = true;
            }
        }

        if (changed2)
            await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Repoints all child entities that reference oldCategoryId to newCategoryId.
    /// </summary>
    private async Task RepointCategoryChildrenAsync(string userId, int oldCategoryId, int newCategoryId, DateTime now)
    {
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId && t.CategoryId == oldCategoryId)
            .ToListAsync();
        foreach (var txn in transactions)
        {
            txn.CategoryId = newCategoryId;
            txn.UpdatedAt = now;
        }

        var budgets = await _db.Budgets
            .Where(b => b.UserId == userId && b.CategoryId == oldCategoryId)
            .ToListAsync();
        foreach (var budget in budgets)
        {
            budget.CategoryId = newCategoryId;
            budget.UpdatedAt = now;
        }

        var recurring = await _db.RecurringTransactions
            .Where(r => r.UserId == userId && r.CategoryId == oldCategoryId)
            .ToListAsync();
        foreach (var item in recurring)
        {
            item.CategoryId = newCategoryId;
            item.UpdatedAt = now;
        }
    }
}
