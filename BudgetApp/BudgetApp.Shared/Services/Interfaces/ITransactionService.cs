using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Handles individual financial transactions (expenses and income).
/// </summary>
public interface ITransactionService
{
    List<Transaction> GetTransactions(int month, int year);

    /// <summary>
    /// Searches transactions across all months by description.
    /// Optionally filters by category. Returns most recent first.
    /// </summary>
    List<Transaction> SearchTransactions(string query, int? categoryId = null, int maxResults = 100);

    void AddTransaction(Transaction transaction);
    void UpdateTransaction(Transaction transaction);
    void DeleteTransaction(int transactionId);
    Transaction? GetTransaction(int transactionId);
}
