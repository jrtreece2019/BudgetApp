using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Manages recurring transactions and processes due items automatically.
/// </summary>
public interface IRecurringTransactionService
{
    List<RecurringTransaction> GetRecurringTransactions();
    RecurringTransaction? GetRecurringTransaction(int id);
    void AddRecurringTransaction(RecurringTransaction recurring);
    void UpdateRecurringTransaction(RecurringTransaction recurring);
    void DeleteRecurringTransaction(int id);
    void ProcessRecurringTransactions();
}
