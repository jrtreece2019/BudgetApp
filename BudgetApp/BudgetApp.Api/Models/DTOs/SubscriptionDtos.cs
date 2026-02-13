namespace BudgetApp.Api.Models.DTOs;

/// <summary>
/// Sent by the client after a successful in-app purchase.
/// Contains the receipt/token from the app store for server-side validation.
/// </summary>
public class ValidateReceiptRequest
{
    /// <summary>
    /// Which store: "apple" or "google".
    /// </summary>
    public string Store { get; set; } = string.Empty;

    /// <summary>
    /// The product ID as defined in App Store Connect / Play Console.
    /// Example: "com.budgetapp.premium.monthly"
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// Apple: the base64-encoded receipt from StoreKit.
    /// Google: the purchase token from Google Play Billing.
    /// </summary>
    public string ReceiptData { get; set; } = string.Empty;

    /// <summary>
    /// Google only: the order ID from the purchase.
    /// Apple: not used (extracted from the receipt during validation).
    /// </summary>
    public string? TransactionId { get; set; }
}

/// <summary>
/// Returned after receipt validation. Tells the client if the subscription
/// is active and when it expires.
/// </summary>
public class SubscriptionStatusDto
{
    /// <summary>
    /// Whether the user has an active premium subscription.
    /// True = all premium features unlocked.
    /// </summary>
    public bool IsPremium { get; set; }

    /// <summary>
    /// The subscription status as a string (e.g., "Active", "Cancelled", "Expired").
    /// </summary>
    public string Status { get; set; } = "None";

    /// <summary>
    /// Which store the subscription was purchased from.
    /// Empty if no subscription.
    /// </summary>
    public string? Store { get; set; }

    /// <summary>
    /// Which product the user is subscribed to.
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    /// When the current period ends. Null if no active subscription.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether auto-renewal is on. If false, the subscription won't
    /// renew after the current period — effectively "cancelled but still active."
    /// </summary>
    public bool AutoRenewing { get; set; }
}
