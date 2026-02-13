namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// A raw transaction imported from Plaid. This is a staging table — when
/// Plaid sends us new transactions, they land here first.
///
/// The ImportService then converts ImportedTransactions into regular
/// Transactions (which get synced to the client). This two-step process
/// exists for several reasons:
///
/// 1. PENDING TRANSACTIONS: Banks report pending transactions that may
///    change or disappear. We track them here with IsPending=true and
///    only create a final Transaction when they post.
///
/// 2. DEDUPLICATION: Plaid can send the same transaction multiple times
///    (e.g., when it transitions from pending to posted). The PlaidTransactionId
///    ensures we don't create duplicates.
///
/// 3. CATEGORIZATION: Plaid provides its own categories (e.g., "Food and Drink > Coffee")
///    which we store here. The user can remap these to their budget categories.
///
/// 4. AUDIT TRAIL: If the user disputes or questions a transaction, we
///    have the original Plaid data to reference.
/// </summary>
public class ImportedTransaction
{
    public int Id { get; set; }

    /// <summary>FK to the user.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>FK to the PlaidAccount this transaction belongs to.</summary>
    public int PlaidAccountId { get; set; }

    /// <summary>
    /// Plaid's unique identifier for this transaction.
    /// Example: "lPNjeW1nR6CDn5okmGQ6hEpMo4lLNoSrzqDje"
    /// Used for deduplication — if we see this ID again, we update rather than insert.
    /// </summary>
    public string PlaidTransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction name/description as reported by the bank.
    /// Example: "STARBUCKS STORE #12345"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount. In Plaid's convention:
    ///   Positive = money leaving the account (expenses/debits)
    ///   Negative = money entering the account (income/credits)
    /// We normalize this when converting to a budget Transaction.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>Date the transaction occurred.</summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Whether this is a pending transaction. Pending transactions haven't
    /// fully posted yet and may change (amount might adjust, or it might
    /// disappear entirely if a hold is released).
    /// </summary>
    public bool IsPending { get; set; }

    /// <summary>
    /// Plaid's primary category for this transaction (e.g., "Food and Drink").
    /// </summary>
    public string? PlaidCategory { get; set; }

    /// <summary>
    /// Plaid's detailed category (e.g., "Food and Drink > Coffee Shop").
    /// </summary>
    public string? PlaidDetailedCategory { get; set; }

    /// <summary>
    /// Merchant name if available (e.g., "Starbucks").
    /// Cleaner than the raw transaction Name which often has extra codes.
    /// </summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// FK to the budget Transaction this was converted into.
    /// Null if not yet converted (e.g., still pending, or user hasn't categorized it).
    /// </summary>
    public int? LinkedTransactionId { get; set; }

    /// <summary>
    /// Whether this imported transaction has been processed (converted into
    /// a budget Transaction). Once processed, we don't re-process it.
    /// </summary>
    public bool IsProcessed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser User { get; set; } = null!;
    public PlaidAccount PlaidAccount { get; set; } = null!;
    public Transaction? LinkedTransaction { get; set; }
}
