namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Represents a single bank account within a PlaidItem (bank connection).
///
/// One bank connection (PlaidItem) can have multiple accounts — for example,
/// a Chase connection might have a checking account and a savings account.
///
/// Each PlaidAccount has a Plaid-assigned account_id that we use when
/// fetching transactions for specific accounts.
///
/// The user can optionally map this account to a Category in the budget app,
/// so imported transactions are automatically categorized.
/// </summary>
public class PlaidAccount
{
    public int Id { get; set; }

    /// <summary>FK to the PlaidItem this account belongs to.</summary>
    public int PlaidItemId { get; set; }

    /// <summary>FK to the user (denormalized for faster queries).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Plaid's unique identifier for this specific account.
    /// Example: "BxBXxLj1m4HMXBm9WZZmCWVbPjX16EHwv99vp"
    /// </summary>
    public string PlaidAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Account name as the bank reports it (e.g., "Plaid Checking", "My Savings").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Official account name if different from Name (some banks have both).
    /// </summary>
    public string? OfficialName { get; set; }

    /// <summary>
    /// Account type: "depository", "credit", "loan", "investment", etc.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Account subtype: "checking", "savings", "credit card", etc.
    /// </summary>
    public string? SubType { get; set; }

    /// <summary>
    /// Last 4 digits of the account number (e.g., "0000").
    /// Plaid only provides the mask, never the full account number.
    /// </summary>
    public string? Mask { get; set; }

    /// <summary>
    /// Current balance as reported by the bank.
    /// </summary>
    public decimal? CurrentBalance { get; set; }

    /// <summary>
    /// Available balance (current minus pending transactions).
    /// </summary>
    public decimal? AvailableBalance { get; set; }

    /// <summary>
    /// Optional FK to a Category. If set, all imported transactions from
    /// this account will be assigned to this category automatically.
    /// Null means "uncategorized" — the user can categorize them later.
    /// </summary>
    public int? DefaultCategoryId { get; set; }

    /// <summary>
    /// Whether to import transactions from this account.
    /// Users might want to exclude certain accounts (e.g., investment accounts).
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PlaidItem Item { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public Category? DefaultCategory { get; set; }
}
