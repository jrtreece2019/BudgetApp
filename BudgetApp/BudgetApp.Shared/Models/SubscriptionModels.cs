namespace BudgetApp.Shared.Models;

/// <summary>
/// Client-side model for subscription status.
/// Mirrors the server's SubscriptionStatusDto.
/// </summary>
public class SubscriptionStatus
{
    /// <summary>True if the user has an active premium subscription.</summary>
    public bool IsPremium { get; set; }

    /// <summary>"Active", "Cancelled", "Expired", "None", etc.</summary>
    public string Status { get; set; } = "None";

    /// <summary>Which store: "apple", "google", or null if no subscription.</summary>
    public string? Store { get; set; }

    /// <summary>Which product (e.g., "com.budgetapp.premium.monthly").</summary>
    public string? ProductId { get; set; }

    /// <summary>When the current period ends (null if no subscription).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Whether auto-renewal is on.</summary>
    public bool AutoRenewing { get; set; }
}

/// <summary>
/// Represents a subscription plan shown in the upgrade UI.
/// </summary>
public class SubscriptionPlan
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string? Savings { get; set; }
    public bool IsPopular { get; set; }
}
