using BudgetApp.Shared.Helpers;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// SQLite-backed implementation of IRecurringTransactionService.
/// Uses the shared RecurrenceCalculator for due-date math.
/// </summary>
public class SqliteRecurringTransactionService : IRecurringTransactionService
{
    private readonly IDatabaseService _db;

    public SqliteRecurringTransactionService(IDatabaseService db)
    {
        _db = db;
    }

    public List<RecurringTransaction> GetRecurringTransactions()
        => _db.GetRecurringTransactions();

    public RecurringTransaction? GetRecurringTransaction(int id)
        => _db.GetRecurringTransaction(id);

    public void AddRecurringTransaction(RecurringTransaction recurring)
        => _db.AddRecurringTransaction(recurring);

    public void UpdateRecurringTransaction(RecurringTransaction recurring)
        => _db.UpdateRecurringTransaction(recurring);

    public void DeleteRecurringTransaction(int id)
        => _db.DeleteRecurringTransaction(id);

    /// <summary>
    /// Scans all active recurring transactions, generates any past-due
    /// transactions, and advances the NextDueDate forward.
    /// </summary>
    public void ProcessRecurringTransactions()
    {
        var today = DateTime.Today;
        var recurringList = _db.GetRecurringTransactions()
            .Where(r => r.IsActive && r.NextDueDate <= today)
            .ToList();

        foreach (var recurring in recurringList)
        {
            while (recurring.NextDueDate <= today)
            {
                var transaction = new Transaction
                {
                    Description = recurring.Description,
                    Amount = recurring.Amount,
                    CategoryId = recurring.CategoryId,
                    Type = recurring.Type,
                    Date = recurring.NextDueDate
                };
                _db.AddTransaction(transaction);

                recurring.NextDueDate = RecurrenceCalculator.CalculateNextDueDate(recurring);
            }

            _db.UpdateRecurringTransaction(recurring);
        }
    }
}
