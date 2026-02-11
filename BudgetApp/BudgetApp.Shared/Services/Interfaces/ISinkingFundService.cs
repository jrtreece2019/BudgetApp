using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Manages sinking funds (savings goals) and their contribution/withdrawal transactions.
/// </summary>
public interface ISinkingFundService
{
    List<SinkingFund> GetSinkingFunds();
    SinkingFund? GetSinkingFund(int id);
    void AddSinkingFund(SinkingFund fund);
    void UpdateSinkingFund(SinkingFund fund);
    void DeleteSinkingFund(int id);
    List<SinkingFundTransaction> GetSinkingFundTransactions(int fundId);
    void AddSinkingFundTransaction(SinkingFundTransaction transaction);
    void DeleteSinkingFundTransaction(int transactionId);
    decimal GetTotalSinkingFundContributions(int month, int year);
}
