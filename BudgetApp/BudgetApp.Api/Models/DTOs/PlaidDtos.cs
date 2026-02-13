namespace BudgetApp.Api.Models.DTOs;

// ── Link Token ──────────────────────────────────────────────────

/// <summary>
/// Returned when the client requests a Plaid Link token.
/// The client uses this token to open the Plaid Link UI (the widget
/// where users select their bank and log in).
/// </summary>
public class CreateLinkTokenResponse
{
    public string LinkToken { get; set; } = string.Empty;
}

// ── Public Token Exchange ───────────────────────────────────────

/// <summary>
/// Sent by the client after the user finishes Plaid Link.
/// Plaid Link gives the client a temporary "public token" which our API
/// exchanges for a permanent access token via Plaid's API.
/// </summary>
public class ExchangePublicTokenRequest
{
    public string PublicToken { get; set; } = string.Empty;
}

/// <summary>
/// Returned after successfully exchanging the public token.
/// Contains the linked accounts so the UI can show what was connected.
/// </summary>
public class ExchangePublicTokenResponse
{
    public int PlaidItemId { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public List<PlaidAccountDto> Accounts { get; set; } = new();
}

// ── Connected Accounts ──────────────────────────────────────────

/// <summary>
/// A simplified view of a PlaidAccount for the client.
/// No sensitive data (access tokens, Plaid IDs) is exposed.
/// </summary>
public class PlaidAccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OfficialName { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? SubType { get; set; }
    public string? Mask { get; set; }
    public decimal? CurrentBalance { get; set; }
    public decimal? AvailableBalance { get; set; }
    public bool IsEnabled { get; set; }
    public int? DefaultCategoryId { get; set; }
}

/// <summary>
/// A summary of a connected bank (PlaidItem) for the client UI.
/// Shows institution name, status, and the accounts within it.
/// </summary>
public class ConnectedBankDto
{
    public int Id { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime ConnectedAt { get; set; }
    public List<PlaidAccountDto> Accounts { get; set; } = new();
}

// ── Imported Transactions ───────────────────────────────────────

/// <summary>
/// View of an imported (Plaid) transaction for the client.
/// Used on a review screen where the user can categorize and approve
/// imported transactions before they become official budget transactions.
/// </summary>
public class ImportedTransactionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public bool IsPending { get; set; }
    public string? PlaidCategory { get; set; }
    public string? MerchantName { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
}

// ── Plaid Webhook ───────────────────────────────────────────────

/// <summary>
/// The shape of webhook payloads Plaid sends to our endpoint.
/// Plaid sends different webhook types; the two we care about most are:
///   - TRANSACTIONS: new transactions available, or transactions updated/removed
///   - ITEM: connection status changed (e.g., login required)
/// </summary>
public class PlaidWebhookPayload
{
    public string WebhookType { get; set; } = string.Empty;
    public string WebhookCode { get; set; } = string.Empty;
    public string? ItemId { get; set; }
    public string? Error { get; set; }
    public int NewTransactions { get; set; }
}
