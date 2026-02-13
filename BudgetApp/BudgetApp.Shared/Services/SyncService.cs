using System.Net.Http.Json;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// Client-side sync service. Coordinates between local SQLite (via IDatabaseService)
/// and the remote API (via HttpClient + IAuthService for JWT tokens).
///
/// The sync flow is:
/// 1. Get local changes since last sync from DatabaseService.GetChangesSince()
/// 2. Convert local models to sync DTOs (SyncId-based, no local integer Ids)
/// 3. POST to /api/sync with the changes + lastSyncedAt
/// 4. Receive server changes back
/// 5. Convert server DTOs back to local models
/// 6. Apply via DatabaseService.ApplyServerChanges()
/// 7. Update LastSyncedAt
///
/// If the user isn't authenticated, SyncAsync() is a no-op and returns false.
/// </summary>
public class SyncService : ISyncService
{
    private readonly IDatabaseService _db;
    private readonly IAuthService _auth;
    private readonly HttpClient _http;
    private Timer? _autoSyncTimer;

    /// <summary>
    /// Guarantees only one sync runs at a time, even when called from
    /// multiple threads (e.g., UI thread + Timer thread-pool callback).
    /// A plain bool was racy -- two threads could both read false before
    /// either wrote true, letting overlapping syncs slip through.
    /// SemaphoreSlim(1,1) acts as a mutex: only one caller can hold it.
    /// </summary>
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public SyncService(IDatabaseService db, IAuthService auth, HttpClient http)
    {
        _db = db;
        _auth = auth;
        _http = http;
    }

    public bool IsSyncing { get; private set; }

    /// <summary>
    /// Loaded from the persisted UserSettings.LastSyncedAt on first use,
    /// and written back after every successful sync. This ensures the
    /// timestamp survives app restarts so we do incremental (not full)
    /// syncs after the first one.
    /// </summary>
    public DateTime LastSyncedAt
    {
        get => _lastSyncedAt ??= _db.GetLastSyncedAt();
        private set
        {
            _lastSyncedAt = value;
            _db.SetLastSyncedAt(value);
        }
    }
    private DateTime? _lastSyncedAt;

    public async Task<bool> SyncAsync()
    {
        // Don't sync if user isn't logged in.
        if (!_auth.IsAuthenticated)
            return false;

        // Try to acquire the lock without blocking.
        // If another sync is already running, return immediately.
        // We use WaitAsync(0) instead of Wait(0) because the synchronous
        // Wait is not supported on browser platforms (Blazor WASM).
        if (!await _syncLock.WaitAsync(0))
            return false;

        IsSyncing = true;

        try
        {
            // Step 1: Get a valid access token (auto-refreshes if expired).
            var token = await _auth.GetAccessTokenAsync();
            if (token == null) return false;

            // Step 2: Gather local changes since last sync.
            var localChanges = _db.GetChangesSince(LastSyncedAt);

            // Step 3: Build the API request with SyncId-based FK references.
            var apiRequest = BuildApiSyncRequest(localChanges);

            // Add the JWT to the request header.
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Step 4: Send to the server.
            var response = await _http.PostAsJsonAsync("api/sync", apiRequest);

            if (!response.IsSuccessStatusCode)
                return false;

            // Step 5: Deserialize server response (uses SyncId-based DTO format).
            var syncResponse = await response.Content.ReadFromJsonAsync<SyncResponseDto>();
            if (syncResponse == null) return false;

            // Step 6: Apply server changes to local SQLite.
            //
            // We must apply "parent" entities (Categories, SinkingFunds) FIRST
            // because child entities (Transactions, Budgets, etc.) hold integer
            // FK references that we resolve by looking up the parent's SyncId
            // in local SQLite.  If the parent isn't there yet, the FK resolves
            // to 0 and the data is wrong.
            //
            // IMPORTANT: We apply each group exactly ONCE to avoid duplicates.
            // Previously, categories were applied in a separate pass and then
            // again inside ConvertServerResponse, which could cause duplicates
            // if the UpdatedAt comparison behaved unexpectedly across
            // SQLite and JSON DateTime formats.

            // Phase 1: Categories and SinkingFunds (parents)
            var serverChanges = ConvertServerResponse(syncResponse);

            var parentPayload = new SyncPayload
            {
                Categories = serverChanges.Categories,
                SinkingFunds = serverChanges.SinkingFunds
            };
            _db.ApplyServerChanges(parentPayload);

            // Phase 2: Everything else (children that reference parents)
            var childPayload = new SyncPayload
            {
                Transactions = serverChanges.Transactions,
                Budgets = serverChanges.Budgets,
                RecurringTransactions = serverChanges.RecurringTransactions,
                Settings = serverChanges.Settings,
                SinkingFundTransactions = serverChanges.SinkingFundTransactions
            };
            _db.ApplyServerChanges(childPayload);

            // Step 7: Update the sync timestamp.
            LastSyncedAt = syncResponse.SyncedAt;

            return true;
        }
        catch (HttpRequestException)
        {
            // Offline or server unreachable -- that's fine, we'll try again later.
            return false;
        }
        finally
        {
            IsSyncing = false;
            _syncLock.Release();
        }
    }

    public void StartAutoSync(TimeSpan interval)
    {
        StopAutoSync();
        // dueTime = interval (NOT Zero).  The caller is expected to invoke
        // SyncAsync() explicitly for the immediate first sync; the timer
        // handles subsequent periodic syncs only.  Using Zero here would
        // fire the timer callback at the same instant as the explicit call,
        // doubling the first sync.
        _autoSyncTimer = new Timer(
            async _ => await SyncAsync(),
            null,
            interval,            // First tick after one full interval
            interval);           // Then repeat at the same interval
    }

    public void StopAutoSync()
    {
        _autoSyncTimer?.Dispose();
        _autoSyncTimer = null;
    }

    // ── DTO Conversion: Local Models → Sync DTOs ────────────────
    // The API uses SyncId-based references (CategorySyncId instead of CategoryId).
    // We need to look up the Category's SyncId from its local integer Id.

    /// <summary>
    /// Converts local SQLite models to the DTO format the API expects.
    /// The key transformation: integer FKs (CategoryId) → Guid FKs (CategorySyncId).
    /// We need all categories loaded to do the lookup.
    /// </summary>
    private SyncPayload ConvertToSyncPayload(SyncPayload local)
    {
        // Build lookups: local CategoryId → SyncId, SinkingFundId → SyncId.
        // We need ALL categories (including deleted) for FK resolution.
        var allCategories = _db.GetChangesSince(DateTime.MinValue).Categories;
        var categoryIdToSyncId = allCategories.ToDictionary(c => c.Id, c => c.SyncId);

        var allFunds = _db.GetChangesSince(DateTime.MinValue).SinkingFunds;
        var fundIdToSyncId = allFunds.ToDictionary(f => f.Id, f => f.SyncId);

        // The SyncPayload from local uses the same models but the API expects
        // DTOs with SyncId-based references. We return the same SyncPayload
        // since it will be serialized to JSON -- the server DTOs have matching
        // property names (SyncId, UpdatedAt, IsDeleted, etc.)
        //
        // However, Transactions, Budgets, RecurringTransactions, and
        // SinkingFundTransactions have integer FKs that need to be mapped.
        // The API sync DTOs use CategorySyncId/SinkingFundSyncId.
        //
        // Since the HTTP serialization goes through the SyncRequest DTO format
        // defined on the server (which uses CategorySyncId, not CategoryId),
        // we create a JSON-compatible payload using anonymous objects.
        //
        // Actually, the cleanest approach: we pass the SyncPayload as-is and
        // let the server-side DTOs handle the mapping. But the server expects
        // CategorySyncId, not CategoryId.
        //
        // Let's build the API request format directly.
        return local; // The SyncPayload is serialized; the server reads SyncDtos.
        // Note: This works because the server SyncRequest uses SyncPayloadDto
        // which has CategorySyncId. We need to restructure this.
        // See ConvertToApiRequest below.
    }

    /// <summary>
    /// Builds the API request body with proper SyncId-based FK references.
    /// This is what actually gets sent over the wire.
    /// </summary>
    internal object BuildApiSyncRequest(SyncPayload localChanges)
    {
        // Build lookups for FK resolution.
        var fullData = _db.GetChangesSince(DateTime.MinValue);
        var catLookup = fullData.Categories.ToDictionary(c => c.Id, c => c.SyncId);
        var fundLookup = fullData.SinkingFunds.ToDictionary(f => f.Id, f => f.SyncId);

        Guid LookupCat(int id) => catLookup.GetValueOrDefault(id, Guid.Empty);
        Guid LookupFund(int id) => fundLookup.GetValueOrDefault(id, Guid.Empty);

        return new
        {
            LastSyncedAt = LastSyncedAt,
            ClientChanges = new
            {
                Categories = localChanges.Categories.Select(c => new
                {
                    c.SyncId, c.Name, c.Icon, c.Color, c.DefaultBudget,
                    Type = (int)c.Type, c.UpdatedAt, c.IsDeleted
                }),
                Transactions = localChanges.Transactions.Select(t => new
                {
                    t.SyncId, t.Description, t.Amount, t.Date,
                    CategorySyncId = LookupCat(t.CategoryId),
                    Type = (int)t.Type, t.UpdatedAt, t.IsDeleted
                }),
                Budgets = localChanges.Budgets.Select(b => new
                {
                    b.SyncId,
                    CategorySyncId = LookupCat(b.CategoryId),
                    b.Amount, b.Month, b.Year, b.UpdatedAt, b.IsDeleted
                }),
                RecurringTransactions = localChanges.RecurringTransactions.Select(r => new
                {
                    r.SyncId, r.Description, r.Amount,
                    CategorySyncId = LookupCat(r.CategoryId),
                    Type = (int)r.Type, Frequency = (int)r.Frequency,
                    r.DayOfMonth, r.StartDate, r.NextDueDate, r.IsActive,
                    r.UpdatedAt, r.IsDeleted
                }),
                Settings = localChanges.Settings == null ? null : new
                {
                    localChanges.Settings.SyncId,
                    localChanges.Settings.MonthlyIncome,
                    localChanges.Settings.UpdatedAt,
                    localChanges.Settings.IsDeleted
                },
                SinkingFunds = localChanges.SinkingFunds.Select(f => new
                {
                    f.SyncId, f.Name, f.Icon, f.Color,
                    f.GoalAmount, f.CurrentBalance, f.MonthlyContribution,
                    f.StartDate, f.TargetDate, Status = (int)f.Status,
                    f.AutoContribute, f.LastAutoContributeDate,
                    f.UpdatedAt, f.IsDeleted
                }),
                SinkingFundTransactions = localChanges.SinkingFundTransactions.Select(t => new
                {
                    t.SyncId,
                    SinkingFundSyncId = LookupFund(t.SinkingFundId),
                    t.Date, t.Amount, Type = (int)t.Type, t.Note,
                    t.UpdatedAt, t.IsDeleted
                })
            }
        };
    }

    /// <summary>
    /// Converts the server's response (DTO format with SyncId-based FKs)
    /// back to local models (with integer FKs resolved from SyncId lookups).
    /// </summary>
    private SyncPayload ConvertFromSyncPayload(SyncPayload serverPayload)
    {
        // Server changes arrive as local model types (thanks to shared SyncPayload).
        // But Transaction.CategoryId will be 0 -- we need to resolve it from
        // the category's SyncId. However, the server response uses the SyncPayload
        // format which doesn't have CategorySyncId in the model -- it has CategoryId.
        //
        // We handle this in the overloaded method below that works with the
        // JSON-deserialized SyncResponse.
        return serverPayload;
    }

    /// <summary>
    /// Converts server sync response DTOs (with SyncId-based FKs) into local models.
    /// Resolves CategorySyncId → local CategoryId using the local database.
    /// </summary>
    internal SyncPayload ConvertServerResponse(SyncResponseDto response)
    {
        // Load all local categories and funds to resolve SyncId → local Id.
        var fullData = _db.GetChangesSince(DateTime.MinValue);
        var catSyncToId = fullData.Categories.ToDictionary(c => c.SyncId, c => c.Id);
        var fundSyncToId = fullData.SinkingFunds.ToDictionary(f => f.SyncId, f => f.Id);

        int LookupCatId(Guid syncId) => catSyncToId.GetValueOrDefault(syncId, 0);
        int LookupFundId(Guid syncId) => fundSyncToId.GetValueOrDefault(syncId, 0);

        var payload = response.ServerChanges;

        return new SyncPayload
        {
            Categories = payload.Categories.Select(c => new Category
            {
                SyncId = c.SyncId, Name = c.Name, Icon = c.Icon, Color = c.Color,
                DefaultBudget = c.DefaultBudget, Type = (CategoryType)c.Type,
                UpdatedAt = c.UpdatedAt, IsDeleted = c.IsDeleted
            }).ToList(),
            Transactions = payload.Transactions.Select(t => new Transaction
            {
                SyncId = t.SyncId, Description = t.Description, Amount = t.Amount,
                Date = t.Date, CategoryId = LookupCatId(t.CategorySyncId),
                Type = (TransactionType)t.Type, UpdatedAt = t.UpdatedAt,
                IsDeleted = t.IsDeleted
            }).ToList(),
            Budgets = payload.Budgets.Select(b => new Budget
            {
                SyncId = b.SyncId, CategoryId = LookupCatId(b.CategorySyncId),
                Amount = b.Amount, Month = b.Month, Year = b.Year,
                UpdatedAt = b.UpdatedAt, IsDeleted = b.IsDeleted
            }).ToList(),
            RecurringTransactions = payload.RecurringTransactions.Select(r => new RecurringTransaction
            {
                SyncId = r.SyncId, Description = r.Description, Amount = r.Amount,
                CategoryId = LookupCatId(r.CategorySyncId),
                Type = (TransactionType)r.Type, Frequency = (RecurrenceFrequency)r.Frequency,
                DayOfMonth = r.DayOfMonth, StartDate = r.StartDate,
                NextDueDate = r.NextDueDate, IsActive = r.IsActive,
                UpdatedAt = r.UpdatedAt, IsDeleted = r.IsDeleted
            }).ToList(),
            Settings = payload.Settings == null ? null : new UserSettings
            {
                SyncId = payload.Settings.SyncId,
                MonthlyIncome = payload.Settings.MonthlyIncome,
                UpdatedAt = payload.Settings.UpdatedAt,
                IsDeleted = payload.Settings.IsDeleted
            },
            SinkingFunds = payload.SinkingFunds.Select(f => new SinkingFund
            {
                SyncId = f.SyncId, Name = f.Name, Icon = f.Icon, Color = f.Color,
                GoalAmount = f.GoalAmount, CurrentBalance = f.CurrentBalance,
                MonthlyContribution = f.MonthlyContribution,
                StartDate = f.StartDate, TargetDate = f.TargetDate,
                Status = (SinkingFundStatus)f.Status,
                AutoContribute = f.AutoContribute,
                LastAutoContributeDate = f.LastAutoContributeDate,
                UpdatedAt = f.UpdatedAt, IsDeleted = f.IsDeleted
            }).ToList(),
            SinkingFundTransactions = payload.SinkingFundTransactions.Select(t => new SinkingFundTransaction
            {
                SyncId = t.SyncId, SinkingFundId = LookupFundId(t.SinkingFundSyncId),
                Date = t.Date, Amount = t.Amount,
                Type = (SinkingFundTransactionType)t.Type, Note = t.Note,
                UpdatedAt = t.UpdatedAt, IsDeleted = t.IsDeleted
            }).ToList()
        };
    }
}

// ── Client-side DTOs for deserializing the server response ──────
// These mirror the server's SyncDtos but live in BudgetApp.Shared so the
// client can deserialize the API response. The server uses its own copies.

public class SyncResponseDto
{
    public SyncPayloadDtoClient ServerChanges { get; set; } = new();
    public DateTime SyncedAt { get; set; }
}

public class SyncPayloadDtoClient
{
    public List<CategoryDtoClient> Categories { get; set; } = new();
    public List<TransactionDtoClient> Transactions { get; set; } = new();
    public List<BudgetDtoClient> Budgets { get; set; } = new();
    public List<RecurringTransactionDtoClient> RecurringTransactions { get; set; } = new();
    public UserSettingsDtoClient? Settings { get; set; }
    public List<SinkingFundDtoClient> SinkingFunds { get; set; } = new();
    public List<SinkingFundTransactionDtoClient> SinkingFundTransactions { get; set; } = new();
}

public class CategoryDtoClient
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal DefaultBudget { get; set; }
    public int Type { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class TransactionDtoClient
{
    public Guid SyncId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Guid CategorySyncId { get; set; }
    public int Type { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class BudgetDtoClient
{
    public Guid SyncId { get; set; }
    public Guid CategorySyncId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class RecurringTransactionDtoClient
{
    public Guid SyncId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid CategorySyncId { get; set; }
    public int Type { get; set; }
    public int Frequency { get; set; }
    public int DayOfMonth { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class UserSettingsDtoClient
{
    public Guid SyncId { get; set; }
    public decimal MonthlyIncome { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class SinkingFundDtoClient
{
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal GoalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal MonthlyContribution { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public int Status { get; set; }
    public bool AutoContribute { get; set; }
    public DateTime? LastAutoContributeDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class SinkingFundTransactionDtoClient
{
    public Guid SyncId { get; set; }
    public Guid SinkingFundSyncId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int Type { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
