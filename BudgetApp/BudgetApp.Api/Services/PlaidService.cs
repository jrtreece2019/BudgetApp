using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BudgetApp.Api.Data;
using BudgetApp.Api.Models.DTOs;
using BudgetApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetApp.Api.Services;

/// <summary>
/// Wraps the Plaid REST API. All Plaid communication goes through this service.
///
/// PLAID API BASICS (for a junior dev):
/// - Plaid is a middleman between your app and the user's bank.
/// - Every request to Plaid needs your client_id and secret (like an API key).
/// - Plaid has 3 environments: sandbox (fake data), development (real banks, limited),
///   and production (real banks, unlimited).
/// - The base URL changes per environment:
///   sandbox  → https://sandbox.plaid.com
///   development → https://development.plaid.com
///   production → https://production.plaid.com
///
/// This service uses HttpClient to make raw HTTP requests to Plaid's REST API.
/// We could use a NuGet package, but doing it manually helps you understand
/// what's actually happening over the wire.
/// </summary>
public class PlaidService
{
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PlaidService> _logger;

    // Plaid credentials -- loaded from appsettings.json.
    private readonly string _clientId;
    private readonly string _secret;
    private readonly string _environment;
    private readonly string _webhookUrl;
    private readonly string _baseUrl;

    public PlaidService(
        HttpClient http,
        AppDbContext db,
        IConfiguration config,
        ILogger<PlaidService> logger)
    {
        _http = http;
        _db = db;
        _config = config;
        _logger = logger;

        _clientId = config["Plaid:ClientId"] ?? "";
        _secret = config["Plaid:Secret"] ?? "";
        _environment = config["Plaid:Environment"] ?? "sandbox";
        _webhookUrl = config["Plaid:WebhookUrl"] ?? "";

        // Map environment name to Plaid's base URL.
        _baseUrl = _environment.ToLower() switch
        {
            "sandbox" => "https://sandbox.plaid.com",
            "development" => "https://development.plaid.com",
            "production" => "https://production.plaid.com",
            _ => "https://sandbox.plaid.com"
        };
    }

    // ── Link Token ──────────────────────────────────────────────

    /// <summary>
    /// Creates a Plaid Link token. This is step 1 of the bank connection flow.
    /// The client uses this token to open the Plaid Link UI widget.
    ///
    /// Plaid API: POST /link/token/create
    /// Docs: https://plaid.com/docs/api/link/#linktokencreate
    ///
    /// WHY THE PHONE NUMBER FIELDS?
    /// Newer versions of Plaid Link include a phone-verification step before
    /// showing the bank list.  By passing `phone_number_verified_time` we tell
    /// Plaid "this user's phone was already verified by our app", so Link
    /// skips the screen entirely.  In sandbox we use a dummy number; in
    /// production you'd pass the real verified phone from your user profile.
    /// </summary>
    public async Task<string?> CreateLinkTokenAsync(string userId, string? userEmail = null)
    {
        // Build the user object.  Plaid uses this for identity/fraud checks.
        // phone_number_verified_time tells Plaid to skip phone verification.
        var user = new Dictionary<string, object>
        {
            ["client_user_id"] = userId,
            ["phone_number_verified_time"] = DateTime.UtcNow.AddDays(-1).ToString("o")
        };

        // In sandbox, supply a dummy phone so Plaid's validation is happy.
        // In production, you'd pass the real user phone from your profile table.
        if (_environment.Equals("sandbox", StringComparison.OrdinalIgnoreCase))
        {
            user["phone_number"] = "+14155550123";   // Safe sandbox test number
        }

        if (!string.IsNullOrEmpty(userEmail))
        {
            user["email_address"] = userEmail;
            user["email_address_verified_time"] = DateTime.UtcNow.AddDays(-1).ToString("o");
        }

        var requestBody = new
        {
            client_id = _clientId,
            secret = _secret,
            user,
            client_name = "BudgetApp",
            products = new[] { "transactions" },  // We want transaction data.
            country_codes = new[] { "US" },
            language = "en",
            webhook = _webhookUrl                  // Plaid will notify us here.
        };

        var response = await PostToPlaidAsync<PlaidLinkTokenResponse>(
            "/link/token/create", requestBody);

        return response?.LinkToken;
    }

    // ── Token Exchange ──────────────────────────────────────────

    /// <summary>
    /// Exchanges a temporary public token for a permanent access token.
    /// This is step 2 -- after the user finishes Plaid Link, we get a public
    /// token that's only valid for 30 minutes. We exchange it immediately.
    ///
    /// After exchanging, we also fetch the institution info and account details,
    /// then store everything in the database.
    ///
    /// Plaid API: POST /item/public_token/exchange
    /// </summary>
    public async Task<ExchangePublicTokenResponse?> ExchangePublicTokenAsync(
        string userId, string publicToken)
    {
        // Step 1: Exchange the public token for a permanent access token.
        var exchangeBody = new
        {
            client_id = _clientId,
            secret = _secret,
            public_token = publicToken
        };

        var exchangeResponse = await PostToPlaidAsync<PlaidExchangeResponse>(
            "/item/public_token/exchange", exchangeBody);

        if (exchangeResponse == null)
            return null;

        // Step 2: Get the institution info (bank name, etc.).
        var itemBody = new
        {
            client_id = _clientId,
            secret = _secret,
            access_token = exchangeResponse.AccessToken
        };

        var itemResponse = await PostToPlaidAsync<PlaidItemResponse>(
            "/item/get", itemBody);

        var institutionName = "Unknown Bank";
        var institutionId = "";

        if (itemResponse?.Item?.InstitutionId != null)
        {
            // Fetch institution details (name, logo, etc.).
            var instBody = new
            {
                client_id = _clientId,
                secret = _secret,
                institution_id = itemResponse.Item.InstitutionId,
                country_codes = new[] { "US" }
            };

            var instResponse = await PostToPlaidAsync<PlaidInstitutionResponse>(
                "/institutions/get_by_id", instBody);

            institutionName = instResponse?.Institution?.Name ?? "Unknown Bank";
            institutionId = itemResponse.Item.InstitutionId;
        }

        // Step 3: Save the PlaidItem in our database.
        var plaidItem = new PlaidItem
        {
            UserId = userId,
            PlaidItemId = exchangeResponse.ItemId,
            AccessToken = exchangeResponse.AccessToken,
            InstitutionName = institutionName,
            InstitutionId = institutionId,
            IsActive = true
        };

        _db.PlaidItems.Add(plaidItem);
        await _db.SaveChangesAsync();

        // Step 4: Fetch the accounts for this item and save them.
        var accountsBody = new
        {
            client_id = _clientId,
            secret = _secret,
            access_token = exchangeResponse.AccessToken
        };

        var accountsResponse = await PostToPlaidAsync<PlaidAccountsResponse>(
            "/accounts/get", accountsBody);

        var accountDtos = new List<PlaidAccountDto>();

        if (accountsResponse?.Accounts != null)
        {
            foreach (var acct in accountsResponse.Accounts)
            {
                var plaidAccount = new PlaidAccount
                {
                    PlaidItemId = plaidItem.Id,
                    UserId = userId,
                    PlaidAccountId = acct.AccountId,
                    Name = acct.Name,
                    OfficialName = acct.OfficialName,
                    Type = acct.Type,
                    SubType = acct.SubType,
                    Mask = acct.Mask,
                    CurrentBalance = acct.Balances?.Current,
                    AvailableBalance = acct.Balances?.Available,
                    IsEnabled = true
                };

                _db.PlaidAccounts.Add(plaidAccount);
                await _db.SaveChangesAsync(); // Save to get the Id.

                accountDtos.Add(new PlaidAccountDto
                {
                    Id = plaidAccount.Id,
                    Name = plaidAccount.Name,
                    OfficialName = plaidAccount.OfficialName,
                    Type = plaidAccount.Type,
                    SubType = plaidAccount.SubType,
                    Mask = plaidAccount.Mask,
                    CurrentBalance = plaidAccount.CurrentBalance,
                    AvailableBalance = plaidAccount.AvailableBalance,
                    IsEnabled = plaidAccount.IsEnabled
                });
            }
        }

        return new ExchangePublicTokenResponse
        {
            PlaidItemId = plaidItem.Id,
            InstitutionName = institutionName,
            Accounts = accountDtos
        };
    }

    // ── Transaction Sync ────────────────────────────────────────

    /// <summary>
    /// Fetches new transactions from Plaid using the /transactions/sync endpoint.
    /// This is the modern replacement for /transactions/get. It uses a cursor-based
    /// approach: we send our last cursor, Plaid returns only new/modified/removed
    /// transactions since that cursor.
    ///
    /// This method is called:
    ///   - When we receive a TRANSACTIONS webhook from Plaid
    ///   - Optionally on a scheduled timer as a backup
    ///
    /// Plaid API: POST /transactions/sync
    /// Docs: https://plaid.com/docs/api/products/transactions/#transactionssync
    /// </summary>
    public async Task<int> SyncTransactionsAsync(int plaidItemId)
    {
        var item = await _db.PlaidItems
            .Include(i => i.Accounts)
            .FirstOrDefaultAsync(i => i.Id == plaidItemId);

        if (item == null || !item.IsActive)
            return 0;

        // Build account lookup: Plaid account_id → our PlaidAccount.Id
        var accountLookup = item.Accounts.ToDictionary(a => a.PlaidAccountId, a => a.Id);

        var totalImported = 0;
        var hasMore = true;
        var cursor = item.TransactionsCursor;

        // Plaid may paginate results. Keep fetching until hasMore is false.
        while (hasMore)
        {
            var syncBody = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = item.AccessToken,
                cursor = cursor
            };

            var syncResponse = await PostToPlaidAsync<PlaidTransactionSyncResponse>(
                "/transactions/sync", syncBody);

            if (syncResponse == null)
            {
                _logger.LogWarning("Plaid /transactions/sync returned null for item {ItemId}", plaidItemId);
                break;
            }

            // Process ADDED transactions (new ones we haven't seen).
            if (syncResponse.Added != null)
            {
                foreach (var txn in syncResponse.Added)
                {
                    var plaidAccountId = accountLookup.GetValueOrDefault(txn.AccountId);
                    if (plaidAccountId == 0) continue; // Account not tracked.

                    // Check if we already have this transaction (dedup by PlaidTransactionId).
                    var exists = await _db.ImportedTransactions
                        .AnyAsync(t => t.UserId == item.UserId
                                    && t.PlaidTransactionId == txn.TransactionId);
                    if (exists) continue;

                    // Plaid returns dates as plain strings like "2026-02-11" with no
                    // time-zone info.  DateTime.Parse gives Kind=Unspecified, which
                    // PostgreSQL rejects for its "timestamp with time zone" column.
                    // DateTime.SpecifyKind marks the value as UTC so Npgsql is happy.
                    var parsedDate = DateTime.SpecifyKind(DateTime.Parse(txn.Date), DateTimeKind.Utc);

                    _db.ImportedTransactions.Add(new ImportedTransaction
                    {
                        UserId = item.UserId,
                        PlaidAccountId = plaidAccountId,
                        PlaidTransactionId = txn.TransactionId,
                        Name = txn.Name,
                        Amount = txn.Amount,
                        Date = parsedDate,
                        IsPending = txn.Pending,
                        PlaidCategory = txn.PersonalFinanceCategory?.Primary,
                        PlaidDetailedCategory = txn.PersonalFinanceCategory?.Detailed,
                        MerchantName = txn.MerchantName,
                        IsProcessed = false
                    });

                    totalImported++;
                }
            }

            // Process MODIFIED transactions (pending → posted, amount changed).
            if (syncResponse.Modified != null)
            {
                foreach (var txn in syncResponse.Modified)
                {
                    var existing = await _db.ImportedTransactions
                        .FirstOrDefaultAsync(t => t.UserId == item.UserId
                                               && t.PlaidTransactionId == txn.TransactionId);
                    if (existing != null)
                    {
                        existing.Name = txn.Name;
                        existing.Amount = txn.Amount;
                        existing.Date = DateTime.SpecifyKind(DateTime.Parse(txn.Date), DateTimeKind.Utc);
                        existing.IsPending = txn.Pending;
                        existing.MerchantName = txn.MerchantName;
                        existing.PlaidCategory = txn.PersonalFinanceCategory?.Primary;
                        existing.PlaidDetailedCategory = txn.PersonalFinanceCategory?.Detailed;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            // Process REMOVED transactions (Plaid says these no longer exist).
            if (syncResponse.Removed != null)
            {
                foreach (var removed in syncResponse.Removed)
                {
                    var existing = await _db.ImportedTransactions
                        .FirstOrDefaultAsync(t => t.UserId == item.UserId
                                               && t.PlaidTransactionId == removed.TransactionId);
                    if (existing != null)
                    {
                        _db.ImportedTransactions.Remove(existing);
                    }
                }
            }

            // Update the cursor for next time.
            cursor = syncResponse.NextCursor;
            hasMore = syncResponse.HasMore;
        }

        // Save everything and update the cursor.
        item.TransactionsCursor = cursor;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Synced {Count} transactions for PlaidItem {ItemId}",
            totalImported, plaidItemId);

        return totalImported;
    }

    // ── Process Imported Transactions ───────────────────────────

    /// <summary>
    /// Converts unprocessed ImportedTransactions into regular budget Transactions.
    ///
    /// This is where Plaid data becomes part of the user's budget. For each
    /// imported transaction that hasn't been processed yet:
    /// 1. Skip if it's still pending (wait for it to post).
    /// 2. Determine the category (from the account's default, or uncategorized).
    /// 3. Create a regular Transaction with a SyncId so it gets synced to the client.
    /// 4. Mark the ImportedTransaction as processed.
    ///
    /// In the future, you could add smart categorization here (matching merchant
    /// names to categories, ML-based classification, etc.).
    /// </summary>
    public async Task<int> ProcessImportedTransactionsAsync(string userId)
    {
        var unprocessed = await _db.ImportedTransactions
            .Include(t => t.PlaidAccount)
            .Where(t => t.UserId == userId && !t.IsProcessed && !t.IsPending)
            .ToListAsync();

        if (unprocessed.Count == 0) return 0;

        // We need a fallback category for imported transactions that don't have
        // a default set.  Look for an "Uncategorized" category owned by this user.
        // If it doesn't exist, create one.  This avoids FK violations when
        // CategoryId would otherwise be 0 (which doesn't exist in the table).
        var fallbackCategory = await _db.Categories
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Name == "Uncategorized");

        if (fallbackCategory == null)
        {
            fallbackCategory = new Category
            {
                SyncId = Guid.NewGuid(),
                UserId = userId,
                Name = "Uncategorized",
                Icon = "❓",
                Color = "#6B7280",
                UpdatedAt = DateTime.UtcNow
            };
            _db.Categories.Add(fallbackCategory);
            await _db.SaveChangesAsync(); // Save to get the auto-generated Id.
        }

        var processed = 0;

        foreach (var imported in unprocessed)
        {
            // Determine the category: use the account's default, or fallback.
            var categoryId = imported.PlaidAccount?.DefaultCategoryId ?? fallbackCategory.Id;

            // Plaid amounts: positive = expense, negative = income.
            var type = imported.Amount >= 0
                ? TransactionType.Expense
                : TransactionType.Income;
            var amount = Math.Abs(imported.Amount);

            // Create the budget transaction.
            var transaction = new Transaction
            {
                SyncId = Guid.NewGuid(),
                UserId = userId,
                Description = imported.MerchantName ?? imported.Name,
                Amount = amount,
                Date = imported.Date,
                CategoryId = categoryId,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync(); // Save to get the Id.

            // Link the imported transaction to the new budget transaction.
            imported.LinkedTransactionId = transaction.Id;
            imported.IsProcessed = true;
            imported.UpdatedAt = DateTime.UtcNow;

            processed++;
        }

        await _db.SaveChangesAsync();
        return processed;
    }

    // ── Webhook Handling ────────────────────────────────────────

    /// <summary>
    /// Handles incoming Plaid webhooks. Plaid sends POST requests to our
    /// webhook URL whenever something noteworthy happens.
    ///
    /// Webhook types we handle:
    ///   TRANSACTIONS / SYNC_UPDATES_AVAILABLE → new transactions ready to fetch
    ///   ITEM / ERROR → bank connection has an issue (e.g., login required)
    /// </summary>
    public async Task HandleWebhookAsync(PlaidWebhookPayload payload)
    {
        _logger.LogInformation("Received Plaid webhook: {Type} / {Code} for item {ItemId}",
            payload.WebhookType, payload.WebhookCode, payload.ItemId);

        if (payload.ItemId == null) return;

        var item = await _db.PlaidItems
            .FirstOrDefaultAsync(i => i.PlaidItemId == payload.ItemId);

        if (item == null)
        {
            _logger.LogWarning("Webhook for unknown PlaidItemId: {ItemId}", payload.ItemId);
            return;
        }

        switch (payload.WebhookType.ToUpper())
        {
            case "TRANSACTIONS":
                await HandleTransactionWebhookAsync(item, payload);
                break;

            case "ITEM":
                await HandleItemWebhookAsync(item, payload);
                break;

            default:
                _logger.LogInformation("Ignoring webhook type: {Type}", payload.WebhookType);
                break;
        }
    }

    private async Task HandleTransactionWebhookAsync(PlaidItem item, PlaidWebhookPayload payload)
    {
        switch (payload.WebhookCode.ToUpper())
        {
            case "SYNC_UPDATES_AVAILABLE":
            case "INITIAL_UPDATE":
            case "HISTORICAL_UPDATE":
            case "DEFAULT_UPDATE":
                // New transactions available — fetch them.
                await SyncTransactionsAsync(item.Id);

                // Always try to process unprocessed transactions, even if
                // no NEW transactions arrived this time.  Previous runs may
                // have imported transactions but failed during processing
                // (e.g., missing category).  This makes the pipeline resilient.
                await ProcessImportedTransactionsAsync(item.UserId);
                break;

            case "TRANSACTIONS_REMOVED":
                // Plaid says some transactions were removed — re-sync.
                await SyncTransactionsAsync(item.Id);
                break;
        }
    }

    private async Task HandleItemWebhookAsync(PlaidItem item, PlaidWebhookPayload payload)
    {
        switch (payload.WebhookCode.ToUpper())
        {
            case "ERROR":
                // Bank connection has an error (e.g., user changed their bank password).
                item.IsActive = false;
                item.ErrorCode = payload.Error;
                item.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogWarning("PlaidItem {Id} error: {Error}", item.Id, payload.Error);
                break;

            case "PENDING_EXPIRATION":
                _logger.LogWarning("PlaidItem {Id} access is pending expiration", item.Id);
                break;
        }
    }

    // ── Connected Banks ─────────────────────────────────────────

    /// <summary>
    /// Returns all connected banks and their accounts for a user.
    /// Used by the "Connected Banks" screen in the app.
    /// </summary>
    public async Task<List<ConnectedBankDto>> GetConnectedBanksAsync(string userId)
    {
        var items = await _db.PlaidItems
            .Include(i => i.Accounts)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return items.Select(item => new ConnectedBankDto
        {
            Id = item.Id,
            InstitutionName = item.InstitutionName,
            IsActive = item.IsActive,
            ErrorCode = item.ErrorCode,
            ConnectedAt = item.CreatedAt,
            Accounts = item.Accounts.Select(a => new PlaidAccountDto
            {
                Id = a.Id,
                Name = a.Name,
                OfficialName = a.OfficialName,
                Type = a.Type,
                SubType = a.SubType,
                Mask = a.Mask,
                CurrentBalance = a.CurrentBalance,
                AvailableBalance = a.AvailableBalance,
                IsEnabled = a.IsEnabled,
                DefaultCategoryId = a.DefaultCategoryId
            }).ToList()
        }).ToList();
    }

    /// <summary>
    /// Disconnects a bank by removing the Plaid item. Calls Plaid to
    /// invalidate the access token, then removes the item from our database.
    /// </summary>
    public async Task<bool> DisconnectBankAsync(string userId, int plaidItemId)
    {
        var item = await _db.PlaidItems
            .FirstOrDefaultAsync(i => i.Id == plaidItemId && i.UserId == userId);

        if (item == null) return false;

        // Tell Plaid to invalidate the access token.
        try
        {
            var removeBody = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = item.AccessToken
            };
            await PostToPlaidAsync<object>("/item/remove", removeBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove Plaid item remotely; removing locally anyway");
        }

        // Remove from our database (cascades to accounts and imported transactions).
        _db.PlaidItems.Remove(item);
        await _db.SaveChangesAsync();

        return true;
    }

    // ── Unprocessed Transactions ────────────────────────────────

    /// <summary>
    /// Returns imported transactions that haven't been converted to budget
    /// transactions yet. Used by a "Review Transactions" screen.
    /// </summary>
    public async Task<List<ImportedTransactionDto>> GetUnprocessedTransactionsAsync(string userId)
    {
        return await _db.ImportedTransactions
            .Include(t => t.PlaidAccount)
            .Where(t => t.UserId == userId && !t.IsProcessed)
            .OrderByDescending(t => t.Date)
            .Select(t => new ImportedTransactionDto
            {
                Id = t.Id,
                Name = t.Name,
                Amount = t.Amount,
                Date = t.Date,
                IsPending = t.IsPending,
                PlaidCategory = t.PlaidCategory,
                MerchantName = t.MerchantName,
                AccountName = t.PlaidAccount.Name,
                IsProcessed = t.IsProcessed
            })
            .ToListAsync();
    }

    // ── Plaid API Helper ────────────────────────────────────────

    /// <summary>
    /// Generic helper to POST JSON to the Plaid API and deserialize the response.
    /// All Plaid endpoints use POST with JSON body.
    /// </summary>
    private async Task<T?> PostToPlaidAsync<T>(string endpoint, object body)
    {
        try
        {
            var response = await _http.PostAsJsonAsync(
                $"{_baseUrl}{endpoint}",
                body,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Plaid API error at {Endpoint}: {Status} - {Body}",
                    endpoint, response.StatusCode, errorBody);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Plaid API at {Endpoint}", endpoint);
            return default;
        }
    }
}

// ── Internal DTOs for deserializing Plaid API responses ─────────
// These are only used inside PlaidService. They match the shape of
// Plaid's JSON responses using snake_case property names.

internal class PlaidLinkTokenResponse
{
    public string LinkToken { get; set; } = string.Empty;
}

internal class PlaidExchangeResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
}

internal class PlaidItemResponse
{
    public PlaidItemData? Item { get; set; }
}

internal class PlaidItemData
{
    public string? InstitutionId { get; set; }
}

internal class PlaidInstitutionResponse
{
    public PlaidInstitutionData? Institution { get; set; }
}

internal class PlaidInstitutionData
{
    public string Name { get; set; } = string.Empty;
}

internal class PlaidAccountsResponse
{
    public List<PlaidAccountData>? Accounts { get; set; }
}

internal class PlaidAccountData
{
    public string AccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? OfficialName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? SubType { get; set; }
    public string? Mask { get; set; }
    public PlaidBalanceData? Balances { get; set; }
}

internal class PlaidBalanceData
{
    public decimal? Current { get; set; }
    public decimal? Available { get; set; }
}

internal class PlaidTransactionSyncResponse
{
    public List<PlaidTransactionData>? Added { get; set; }
    public List<PlaidTransactionData>? Modified { get; set; }
    public List<PlaidRemovedTransaction>? Removed { get; set; }
    public string NextCursor { get; set; } = string.Empty;
    public bool HasMore { get; set; }
}

internal class PlaidTransactionData
{
    public string TransactionId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Date { get; set; } = string.Empty;
    public bool Pending { get; set; }
    public string? MerchantName { get; set; }
    public PlaidPersonalFinanceCategory? PersonalFinanceCategory { get; set; }
}

internal class PlaidPersonalFinanceCategory
{
    public string? Primary { get; set; }
    public string? Detailed { get; set; }
}

internal class PlaidRemovedTransaction
{
    public string TransactionId { get; set; } = string.Empty;
}
