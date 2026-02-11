using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Handles individual financial transactions (expenses and income).
/// </summary>
public interface ITransactionService
{
    List<Transaction> GetTransactions(int month, int year);
    void AddTransaction(Transaction transaction);
    void UpdateTransaction(Transaction transaction);
    void DeleteTransaction(int transactionId);
    Transaction? GetTransaction(int transactionId);
}
