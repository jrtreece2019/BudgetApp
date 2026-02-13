using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Client-side service for checking and managing the user's subscription.
///
/// Used by:
/// - ConnectBank page: to check if the user can connect banks (premium only)
/// - Upgrade page: to show current status and handle purchases
/// - Any future premium features
///
/// The actual purchase flow (showing the Apple/Google payment sheet) is
/// platform-specific and handled by MAUI. This interface covers the
/// API communication (status check, receipt validation).
///
/// On web, users can't do in-app purchases through Apple/Google — they'd
/// need to subscribe through the mobile app first, or through a web payment
/// gateway like Stripe (which could be added later).
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Cached subscription status. Updated by RefreshStatusAsync().
    /// Check this before showing premium features.
    /// </summary>
    SubscriptionStatus CurrentStatus { get; }

    /// <summary>
    /// Shorthand: is the user currently premium?
    /// Checks the cached status, or fetches it if not loaded yet.
    /// </summary>
    bool IsPremium { get; }

    /// <summary>
    /// Fetches the latest subscription status from the server.
    /// Call this on app startup and after any purchase.
    /// </summary>
    Task<SubscriptionStatus> RefreshStatusAsync();

    /// <summary>
    /// Sends a purchase receipt to the server for validation.
    /// Call this after the native purchase flow completes.
    ///
    /// store: "apple" or "google"
    /// productId: the product identifier
    /// receiptData: the receipt/token from the store
    /// transactionId: the store's transaction ID (optional for Apple)
    ///
    /// Returns the updated subscription status after validation.
    /// </summary>
    Task<SubscriptionStatus> ValidateReceiptAsync(
        string store, string productId, string receiptData, string? transactionId = null);

    /// <summary>
    /// Returns the available subscription plans to show in the upgrade UI.
    /// These are hardcoded in the client since they match what's configured
    /// in App Store Connect and Google Play Console.
    /// </summary>
    List<SubscriptionPlan> GetAvailablePlans();
}
