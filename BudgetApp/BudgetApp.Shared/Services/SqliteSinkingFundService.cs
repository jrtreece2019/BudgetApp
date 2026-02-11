using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// SQLite-backed implementation of ISinkingFundService.
/// Business logic for balance updates lives here (not in DatabaseService).
/// </summary>
public class SqliteSinkingFundService : ISinkingFundService
{
    private readonly IDatabaseService _db;

    public SqliteSinkingFundService(IDatabaseService db)
    {
        _db = db;
    }

    public List<SinkingFund> GetSinkingFunds()
        => _db.GetSinkingFunds();

    public SinkingFund? GetSinkingFund(int id)
        => _db.GetSinkingFund(id);

    public void AddSinkingFund(SinkingFund fund)
        => _db.AddSinkingFund(fund);

    public void UpdateSinkingFund(SinkingFund fund)
        => _db.UpdateSinkingFund(fund);

    public void DeleteSinkingFund(int id)
        => _db.DeleteSinkingFund(id);

    public List<SinkingFundTransaction> GetSinkingFundTransactions(int fundId)
        => _db.GetSinkingFundTransactions(fundId);

    /// <summary>
    /// Inserts the transaction AND updates the fund's balance.
    /// Marks the fund as Completed when the goal is reached.
    /// This business logic was moved here from DatabaseService (SRP).
    /// </summary>
    public void AddSinkingFundTransaction(SinkingFundTransaction transaction)
    {
        _db.AddSinkingFundTransaction(transaction);

        var fund = _db.GetSinkingFund(transaction.SinkingFundId);
        if (fund != null)
        {
            fund.CurrentBalance += transaction.Type == SinkingFundTransactionType.Contribution
                ? transaction.Amount
                : -transaction.Amount;

            if (fund.CurrentBalance >= fund.GoalAmount)
                fund.Status = SinkingFundStatus.Completed;

            _db.UpdateSinkingFund(fund);
        }
    }

    /// <summary>
    /// Deletes the transaction AND reverses the balance change.
    /// Reverts Completed status if balance drops below goal.
    /// </summary>
    public void DeleteSinkingFundTransaction(int transactionId)
    {
        var transaction = _db.GetSinkingFundTransaction(transactionId);
        if (transaction != null)
        {
            var fund = _db.GetSinkingFund(transaction.SinkingFundId);
            if (fund != null)
            {
                fund.CurrentBalance += transaction.Type == SinkingFundTransactionType.Contribution
                    ? -transaction.Amount
                    : transaction.Amount;

                if (fund.CurrentBalance < fund.GoalAmount && fund.Status == SinkingFundStatus.Completed)
                    fund.Status = SinkingFundStatus.Active;

                _db.UpdateSinkingFund(fund);
            }

            _db.DeleteSinkingFundTransaction(transactionId);
        }
    }

    public decimal GetTotalSinkingFundContributions(int month, int year)
        => _db.GetTotalSinkingFundContributions(month, year);
}
