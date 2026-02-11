using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// SQLite-backed implementation of ISettingsService.
/// Manages expected income and calculates actual income from transactions.
/// </summary>
public class SqliteSettingsService : ISettingsService
{
    private readonly IDatabaseService _db;

    public SqliteSettingsService(IDatabaseService db)
    {
        _db = db;
    }

    public decimal GetMonthlyIncome()
        => _db.GetSettings().MonthlyIncome;

    public void SetMonthlyIncome(decimal income)
    {
        var settings = _db.GetSettings();
        settings.MonthlyIncome = income;
        _db.UpdateSettings(settings);
    }

    /// <summary>
    /// Returns the total of all Income-type transactions for the given month.
    /// </summary>
    public decimal GetTotalIncome(int month, int year)
        => _db.GetTransactions(month, year)
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);
}
