using System.Net.Http.Json;
using System.Text.Json;
using BudgetApp.Api.Data;
using BudgetApp.Api.Models.DTOs;
using BudgetApp.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetApp.Api.Services;

/// <summary>
/// Manages subscription validation, status checks, and feature gating.
///
/// THE BIG PICTURE (for a junior dev):
///
/// 1. User taps "Subscribe" in the app
/// 2. The mobile OS (iOS/Android) handles the actual payment
/// 3. The OS gives the app a "receipt" proving the purchase
/// 4. The app sends the receipt to our API
/// 5. Our API calls Apple/Google to verify the receipt is real
/// 6. If valid, we store the subscription in our database
/// 7. The client checks IsPremium() before showing premium features
///
/// WHY NOT JUST TRUST THE CLIENT?
/// Because a hacker could modify the app binary to skip the payment check.
/// Server-side validation ensures only real purchases unlock features.
///
/// RECEIPT VALIDATION:
/// - Apple: POST to https://buy.itunes.apple.com/verifyReceipt (production)
///          or https://sandbox.itunes.apple.com/verifyReceipt (sandbox)
/// - Google: GET https://androidpublisher.googleapis.com/androidpublisher/v3/
///           applications/{packageName}/purchases/subscriptions/{subscriptionId}/tokens/{token}
///
/// For sandbox/development, we use a simplified validation that always succeeds
/// so you can test without real purchases. The TODO markers show where to add
/// real store validation when you're ready for production.
/// </summary>
public class SubscriptionService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        AppDbContext db,
        HttpClient http,
        IConfiguration config,
        ILogger<SubscriptionService> logger)
    {
        _db = db;
        _http = http;
        _config = config;
        _logger = logger;
    }

    // ── Feature Gating ──────────────────────────────────────────

    /// <summary>
    /// The main check: does this user have premium access?
    ///
    /// Returns true if the user has an active (or grace period, or cancelled
    /// but not yet expired) subscription. This is what you call before
    /// allowing access to premium features like Plaid bank connections.
    /// </summary>
    public async Task<bool> IsPremiumAsync(string userId)
    {
        var sub = await GetActiveSubscriptionAsync(userId);
        return sub != null;
    }

    /// <summary>
    /// Gets the user's active subscription, if any.
    /// "Active" means: Status is Active, GracePeriod, or Cancelled
    /// AND PeriodEnd hasn't passed yet.
    /// </summary>
    public async Task<Subscription?> GetActiveSubscriptionAsync(string userId)
    {
        var sub = await _db.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.PeriodEnd)
            .FirstOrDefaultAsync();

        if (sub == null) return null;

        // Check if the subscription is still valid.
        var now = DateTime.UtcNow;

        return sub.Status switch
        {
            SubscriptionStatus.Active => sub,
            SubscriptionStatus.GracePeriod => sub.PeriodEnd.AddDays(16) > now ? sub : null,
            SubscriptionStatus.Cancelled => sub.PeriodEnd > now ? sub : null,
            _ => null // Expired or Revoked
        };
    }

    // ── Subscription Status ─────────────────────────────────────

    /// <summary>
    /// Returns the user's subscription status for the client UI.
    /// </summary>
    public async Task<SubscriptionStatusDto> GetStatusAsync(string userId)
    {
        var sub = await GetActiveSubscriptionAsync(userId);

        if (sub == null)
        {
            return new SubscriptionStatusDto
            {
                IsPremium = false,
                Status = "None"
            };
        }

        return new SubscriptionStatusDto
        {
            IsPremium = true,
            Status = sub.Status.ToString(),
            Store = sub.Store,
            ProductId = sub.ProductId,
            ExpiresAt = sub.PeriodEnd,
            AutoRenewing = sub.AutoRenewing
        };
    }

    // ── Receipt Validation ──────────────────────────────────────

    /// <summary>
    /// Validates a purchase receipt from Apple or Google, and if valid,
    /// creates or updates the subscription record.
    ///
    /// Returns the updated subscription status.
    /// </summary>
    public async Task<SubscriptionStatusDto> ValidateAndSaveAsync(
        string userId, ValidateReceiptRequest request)
    {
        // Validate the receipt with the appropriate store.
        var validation = request.Store.ToLower() switch
        {
            "apple" => await ValidateAppleReceiptAsync(request),
            "google" => await ValidateGoogleReceiptAsync(request),
            _ => null
        };

        if (validation == null)
        {
            _logger.LogWarning("Receipt validation failed for user {UserId}, store {Store}",
                userId, request.Store);
            return new SubscriptionStatusDto { IsPremium = false, Status = "ValidationFailed" };
        }

        // Find existing subscription for this store transaction, or create new.
        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StoreTransactionId == validation.TransactionId);

        if (sub == null)
        {
            sub = new Subscription
            {
                UserId = userId,
                Store = request.Store.ToLower(),
                ProductId = request.ProductId,
                StoreTransactionId = validation.TransactionId,
                Status = SubscriptionStatus.Active,
                PeriodStart = validation.PeriodStart,
                PeriodEnd = validation.PeriodEnd,
                AutoRenewing = validation.AutoRenewing,
                ReceiptData = request.ReceiptData
            };
            _db.Subscriptions.Add(sub);
        }
        else
        {
            // Update existing (renewal, status change, etc.).
            sub.Status = SubscriptionStatus.Active;
            sub.PeriodStart = validation.PeriodStart;
            sub.PeriodEnd = validation.PeriodEnd;
            sub.AutoRenewing = validation.AutoRenewing;
            sub.ReceiptData = request.ReceiptData;
            sub.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return await GetStatusAsync(userId);
    }

    // ── Apple Receipt Validation ────────────────────────────────

    /// <summary>
    /// Validates a receipt with Apple's verifyReceipt endpoint.
    ///
    /// IN DEVELOPMENT/SANDBOX: Returns a mock validation that always succeeds.
    /// This lets you test the flow without a real Apple Developer account.
    ///
    /// IN PRODUCTION: Calls Apple's server to verify the receipt is genuine.
    /// If the production endpoint returns status 21007, it means the receipt
    /// is from the sandbox — retry against the sandbox URL.
    /// </summary>
    private async Task<ReceiptValidationResult?> ValidateAppleReceiptAsync(
        ValidateReceiptRequest request)
    {
        var environment = _config["Subscription:Environment"] ?? "sandbox";

        if (environment == "sandbox")
        {
            // SANDBOX MODE: Accept all receipts for testing.
            _logger.LogInformation("Apple receipt validation: sandbox mode, auto-accepting");

            return new ReceiptValidationResult
            {
                TransactionId = request.TransactionId ?? $"apple_sandbox_{Guid.NewGuid():N}",
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1),
                AutoRenewing = true
            };
        }

        // PRODUCTION: Call Apple's verifyReceipt endpoint.
        var appleUrl = "https://buy.itunes.apple.com/verifyReceipt";
        var sharedSecret = _config["Subscription:AppleSharedSecret"] ?? "";

        var body = new
        {
            // The receipt-data is the base64 receipt from StoreKit.
            receipt_data = request.ReceiptData,    // casing for Apple's API
            password = sharedSecret,
            exclude_old_transactions = true
        };

        try
        {
            var response = await _http.PostAsJsonAsync(appleUrl, body);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            var status = json.GetProperty("status").GetInt32();

            // Status 21007 means it's a sandbox receipt sent to production.
            if (status == 21007)
            {
                appleUrl = "https://sandbox.itunes.apple.com/verifyReceipt";
                response = await _http.PostAsJsonAsync(appleUrl, body);
                json = await response.Content.ReadFromJsonAsync<JsonElement>();
                status = json.GetProperty("status").GetInt32();
            }

            if (status != 0)
            {
                _logger.LogWarning("Apple receipt validation failed with status {Status}", status);
                return null;
            }

            // Parse the latest receipt info.
            var latestInfo = json.GetProperty("latest_receipt_info")
                .EnumerateArray()
                .OrderByDescending(r => r.GetProperty("expires_date_ms").GetString())
                .FirstOrDefault();

            if (latestInfo.ValueKind == JsonValueKind.Undefined)
                return null;

            var expiresMs = long.Parse(latestInfo.GetProperty("expires_date_ms").GetString()!);
            var purchaseMs = long.Parse(latestInfo.GetProperty("purchase_date_ms").GetString()!);
            var originalTxnId = latestInfo.GetProperty("original_transaction_id").GetString()!;

            return new ReceiptValidationResult
            {
                TransactionId = originalTxnId,
                PeriodStart = DateTimeOffset.FromUnixTimeMilliseconds(purchaseMs).UtcDateTime,
                PeriodEnd = DateTimeOffset.FromUnixTimeMilliseconds(expiresMs).UtcDateTime,
                AutoRenewing = !json.TryGetProperty("pending_renewal_info", out var renewal)
                    || renewal.EnumerateArray().All(r =>
                        r.GetProperty("auto_renew_status").GetString() == "1")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple receipt validation error");
            return null;
        }
    }

    // ── Google Receipt Validation ───────────────────────────────

    /// <summary>
    /// Validates a purchase token with Google Play Developer API.
    ///
    /// IN DEVELOPMENT/SANDBOX: Returns a mock validation that always succeeds.
    ///
    /// IN PRODUCTION: Calls Google's Android Publisher API.
    /// Requires a service account JSON key with permissions to verify purchases.
    /// </summary>
    private async Task<ReceiptValidationResult?> ValidateGoogleReceiptAsync(
        ValidateReceiptRequest request)
    {
        var environment = _config["Subscription:Environment"] ?? "sandbox";

        if (environment == "sandbox")
        {
            _logger.LogInformation("Google receipt validation: sandbox mode, auto-accepting");

            return new ReceiptValidationResult
            {
                TransactionId = request.TransactionId ?? $"google_sandbox_{Guid.NewGuid():N}",
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow.AddMonths(1),
                AutoRenewing = true
            };
        }

        // PRODUCTION: Call Google Play Developer API.
        var packageName = _config["Subscription:GooglePackageName"] ?? "";
        var googleUrl = $"https://androidpublisher.googleapis.com/androidpublisher/v3/" +
                        $"applications/{packageName}/purchases/subscriptionsv2/tokens/{request.ReceiptData}";

        // NOTE: This requires an OAuth2 access token from a Google service account.
        // In production, you'd use Google.Apis.AndroidPublisher.v3 NuGet package
        // with a service account key. For now, we use the raw HTTP approach.
        var accessToken = _config["Subscription:GoogleAccessToken"] ?? "";

        try
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _http.GetAsync(googleUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google receipt validation failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Parse the subscription state.
            var expiryTimeMs = json.GetProperty("lineItems")[0]
                .GetProperty("expiryTime").GetString();
            var expiryTime = DateTime.Parse(expiryTimeMs!);

            return new ReceiptValidationResult
            {
                TransactionId = request.TransactionId ?? json.GetProperty("latestOrderId").GetString()!,
                PeriodStart = DateTime.UtcNow, // Google doesn't provide start in v2
                PeriodEnd = expiryTime,
                AutoRenewing = json.TryGetProperty("subscriptionState", out var state)
                    && state.GetString() == "SUBSCRIPTION_STATE_ACTIVE"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google receipt validation error");
            return null;
        }
    }

    // ── Store Webhook Handling ───────────────────────────────────

    /// <summary>
    /// Handles Apple's Server-to-Server notifications (App Store Server Notifications V2).
    /// Apple sends these when a subscription renews, is cancelled, expires, etc.
    /// </summary>
    public async Task HandleAppleNotificationAsync(JsonElement payload)
    {
        // Apple notifications contain a signed JWT. In production, you'd verify
        // the signature. The decoded payload contains:
        // - notificationType: "DID_RENEW", "DID_CHANGE_RENEWAL_STATUS", "EXPIRED", etc.
        // - data.signedTransactionInfo: contains original_transaction_id

        _logger.LogInformation("Received Apple notification");

        // TODO: Implement full Apple notification handling.
        // For now, the easiest approach is to re-validate the receipt
        // when the client opens the app (they send the latest receipt).
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles Google Play's Real-time Developer Notifications (RTDN).
    /// Google sends these via Pub/Sub when subscription events occur.
    /// </summary>
    public async Task HandleGoogleNotificationAsync(JsonElement payload)
    {
        _logger.LogInformation("Received Google notification");

        // TODO: Implement full Google RTDN handling.
        // Google sends a Pub/Sub message with a base64-encoded payload
        // containing subscriptionNotification.purchaseToken.
        await Task.CompletedTask;
    }

    // ── Expiration Check ────────────────────────────────────────

    /// <summary>
    /// Checks all subscriptions and marks expired ones.
    /// Call this on a timer (e.g., once per hour) or on app startup.
    /// </summary>
    public async Task ExpireOverdueSubscriptionsAsync()
    {
        var now = DateTime.UtcNow;
        var graceDays = 16; // Apple gives up to 16 days, Google up to 7.

        var overdueSubscriptions = await _db.Subscriptions
            .Where(s => (s.Status == SubscriptionStatus.Active
                      || s.Status == SubscriptionStatus.GracePeriod)
                     && s.PeriodEnd.AddDays(graceDays) < now)
            .ToListAsync();

        foreach (var sub in overdueSubscriptions)
        {
            sub.Status = SubscriptionStatus.Expired;
            sub.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Expired subscription {Id} for user {UserId}",
                sub.Id, sub.UserId);
        }

        if (overdueSubscriptions.Count > 0)
            await _db.SaveChangesAsync();
    }
}

/// <summary>
/// Internal result from validating a receipt with Apple or Google.
/// </summary>
internal class ReceiptValidationResult
{
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public bool AutoRenewing { get; set; }
}
