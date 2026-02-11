namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Manages user settings like expected monthly income and income tracking.
/// </summary>
public interface ISettingsService
{
    decimal GetMonthlyIncome();
    void SetMonthlyIncome(decimal income);
    decimal GetTotalIncome(int month, int year);
}
