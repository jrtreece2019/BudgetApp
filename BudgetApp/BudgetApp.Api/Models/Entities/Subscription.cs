namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Tracks a user's subscription status.
///
/// HOW IN-APP SUBSCRIPTIONS WORK (for a junior dev):
///
/// When a user subscribes in the mobile app, they pay through Apple (App Store)
/// or Google (Play Store) — your app never handles credit card info directly.
///
/// The store gives the client a "receipt" (Apple) or "purchase token" (Google).
/// The client sends that to our API, and our API calls the store's servers
/// to verify it's real. If valid, we record the subscription here.
///
/// Subscriptions auto-renew. Each store sends server-to-server notifications
/// when a subscription renews, is cancelled, or expires. We update this
/// record accordingly.
///
/// WHY SERVER-SIDE VALIDATION:
/// We can't trust the client alone — a hacker could modify the app to say
/// "I'm subscribed" without paying. Server-side validation ensures only
/// real purchases unlock premium features.
/// </summary>
public class Subscription
{
    public int Id { get; set; }

    /// <summary>FK to the user who owns this subscription.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Which store the subscription was purchased through.
    /// "apple", "google", or "stripe" (for web purchases, if added later).
    /// </summary>
    public string Store { get; set; } = string.Empty;

    /// <summary>
    /// The product identifier as defined in App Store Connect / Google Play Console.
    /// Example: "com.budgetapp.premium.monthly" or "com.budgetapp.premium.yearly"
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// The store's unique identifier for this specific purchase.
    /// Apple: original_transaction_id from the receipt
    /// Google: orderId from the purchase token
    /// Used to match renewal/cancellation notifications to this subscription.
    /// </summary>
    public string StoreTransactionId { get; set; } = string.Empty;

    /// <summary>
    /// The current status of the subscription.
    /// </summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// When the current billing period started.
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// When the current billing period ends. After this date, the subscription
    /// needs to be renewed. If the store doesn't send a renewal notification
    /// by this date + a small grace period, we consider it expired.
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Whether the user has turned off auto-renewal. The subscription stays
    /// active until PeriodEnd, but won't renew after that.
    /// </summary>
    public bool AutoRenewing { get; set; } = true;

    /// <summary>
    /// The raw receipt data from the store. Stored for audit/re-validation.
    /// Apple: the base64-encoded receipt
    /// Google: the purchase token
    /// </summary>
    public string? ReceiptData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser User { get; set; } = null!;
}

/// <summary>
/// Possible states a subscription can be in.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Actively paying, all premium features unlocked.</summary>
    Active = 0,

    /// <summary>
    /// Past the billing date but within a grace period (typically 3-16 days).
    /// Apple and Google give users time to fix payment issues.
    /// Premium features stay unlocked during grace period.
    /// </summary>
    GracePeriod = 1,

    /// <summary>
    /// User cancelled auto-renewal but the current period hasn't ended.
    /// Premium features stay unlocked until PeriodEnd.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Subscription has fully expired. No premium features.
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Payment failed and grace period passed. No premium features.
    /// </summary>
    Revoked = 4
}
