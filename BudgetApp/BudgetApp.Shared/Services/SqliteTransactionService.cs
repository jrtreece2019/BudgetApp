using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// SQLite-backed implementation of ITransactionService.
/// Adds secondary sort by Id so same-day transactions stay stable.
/// </summary>
public class SqliteTransactionService : ITransactionService
{
    private readonly IDatabaseService _db;

    public SqliteTransactionService(IDatabaseService db)
    {
        _db = db;
    }

    public List<Transaction> GetTransactions(int month, int year)
    {
        // DatabaseService already orders by date; add a tiebreaker on Id
        return _db.GetTransactions(month, year)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToList();
    }

    public void AddTransaction(Transaction transaction)
        => _db.AddTransaction(transaction);

    public void UpdateTransaction(Transaction transaction)
        => _db.UpdateTransaction(transaction);

    public void DeleteTransaction(int transactionId)
        => _db.DeleteTransaction(transactionId);

    public Transaction? GetTransaction(int transactionId)
        => _db.GetTransaction(transactionId);
}
