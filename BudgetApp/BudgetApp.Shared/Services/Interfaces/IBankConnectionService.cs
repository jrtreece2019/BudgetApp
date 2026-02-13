using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Manages bank connections through Plaid (or any future provider).
///
/// Named IBankConnectionService (not IPlaidService) because the interface
/// describes WHAT it does, not HOW. If we swap Plaid for MX later,
/// we only change the implementation, not the interface (DIP + OCP).
///
/// FLOW:
/// 1. Client calls GetLinkTokenAsync() to get a Plaid Link token.
/// 2. Client opens Plaid Link UI with that token (JS interop).
/// 3. User picks bank, logs in, selects accounts.
/// 4. Plaid Link returns a public token to the client.
/// 5. Client calls ExchangePublicTokenAsync(publicToken).
/// 6. Server exchanges it for a permanent access token + stores accounts.
/// 7. Plaid sends webhooks → server auto-imports transactions.
/// 8. Client calls GetConnectedBanksAsync() to see linked banks.
/// </summary>
public interface IBankConnectionService
{
    /// <summary>
    /// Gets a Plaid Link token from the server. The client uses this
    /// to initialize the Plaid Link UI widget.
    /// Returns null if the request fails.
    /// </summary>
    Task<string?> GetLinkTokenAsync();

    /// <summary>
    /// Sends the temporary public token to the server after the user
    /// completes Plaid Link. The server exchanges it for a permanent
    /// access token and returns the new bank connection info.
    /// Returns null on failure.
    /// </summary>
    Task<ExchangeTokenResponse?> ExchangePublicTokenAsync(string publicToken);

    /// <summary>
    /// Gets all connected banks and their accounts for the current user.
    /// </summary>
    Task<List<ConnectedBank>> GetConnectedBanksAsync();

    /// <summary>
    /// Disconnects a bank (removes the Plaid item and all its data).
    /// Returns true on success.
    /// </summary>
    Task<bool> DisconnectBankAsync(int bankId);

    /// <summary>
    /// Gets imported transactions that haven't been processed yet.
    /// These are shown in a "Review Transactions" screen.
    /// </summary>
    Task<List<ImportedTransactionView>> GetUnprocessedTransactionsAsync();

    /// <summary>
    /// Tells the server to process all unprocessed imported transactions
    /// (convert them into budget transactions). Returns the count processed.
    /// </summary>
    Task<int> ProcessTransactionsAsync();
}
