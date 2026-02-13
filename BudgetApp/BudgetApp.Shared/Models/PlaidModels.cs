namespace BudgetApp.Shared.Models;

/// <summary>
/// Client-side DTOs for the Plaid endpoints.
/// These match the API's PlaidDtos so JSON serialization works seamlessly.
/// </summary>

/// <summary>
/// Returned by GET /api/plaid/banks.
/// Represents a connected bank institution with its accounts.
/// </summary>
public class ConnectedBank
{
    public int Id { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime ConnectedAt { get; set; }
    public List<BankAccount> Accounts { get; set; } = new();
}

/// <summary>
/// A single bank account within a ConnectedBank.
/// </summary>
public class BankAccount
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
/// An imported transaction waiting to be reviewed / auto-processed.
/// </summary>
public class ImportedTransactionView
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

/// <summary>
/// Response from POST /api/plaid/link-token.
/// </summary>
public class LinkTokenResponse
{
    public string LinkToken { get; set; } = string.Empty;
}

/// <summary>
/// Response from POST /api/plaid/exchange-token.
/// </summary>
public class ExchangeTokenResponse
{
    public int PlaidItemId { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public List<BankAccount> Accounts { get; set; } = new();
}
