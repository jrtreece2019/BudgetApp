namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Represents a user's connection to a single bank through Plaid.
///
/// When a user links their bank account via Plaid Link, Plaid gives us
/// two things: an "item_id" (identifies the bank connection) and an
/// "access_token" (a permanent key to fetch data from that bank).
///
/// One user can have multiple PlaidItems (e.g., Chase + Bank of America).
/// Each PlaidItem can have multiple PlaidAccounts (e.g., checking + savings).
///
/// SECURITY: The AccessToken is sensitive — it grants access to the user's
/// bank data. In production, this should be encrypted at rest (e.g., using
/// Azure Key Vault or column-level encryption).
/// </summary>
public class PlaidItem
{
    public int Id { get; set; }

    /// <summary>FK to the user who owns this bank connection.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Plaid's unique identifier for this bank connection.
    /// Example: "eVBnVMp7zdTJLkRNr33Rs6zr7KNJqBFL9DrE6"
    /// </summary>
    public string PlaidItemId { get; set; } = string.Empty;

    /// <summary>
    /// The permanent access token from Plaid. Used in all subsequent API calls
    /// to fetch transactions, account balances, etc. for this bank connection.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the bank (e.g., "Chase", "Wells Fargo").
    /// Fetched from Plaid's institution data after linking.
    /// </summary>
    public string InstitutionName { get; set; } = string.Empty;

    /// <summary>
    /// Plaid's institution ID (e.g., "ins_3"). Useful if we need to
    /// display the bank's logo or look up institution details.
    /// </summary>
    public string InstitutionId { get; set; } = string.Empty;

    /// <summary>
    /// Cursor for Plaid's /transactions/sync endpoint. Each time we fetch
    /// new transactions, Plaid gives us an updated cursor. On the next call,
    /// we send this cursor to get only NEW transactions since last time.
    /// Think of it like a bookmark in a book — "I read up to here."
    /// </summary>
    public string? TransactionsCursor { get; set; }

    /// <summary>
    /// Whether this connection is healthy. Plaid items can become disconnected
    /// if the user changes their bank password, for example. When that happens
    /// we set this to false and prompt the user to re-authenticate.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Error code from Plaid if the item is in an error state.
    /// Common example: "ITEM_LOGIN_REQUIRED" when the user needs to re-authenticate.
    /// </summary>
    public string? ErrorCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public AppUser User { get; set; } = null!;
    public List<PlaidAccount> Accounts { get; set; } = new();
}
