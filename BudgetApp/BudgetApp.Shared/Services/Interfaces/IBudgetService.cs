using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Manages monthly budget allocations per category, including custom overrides.
/// </summary>
public interface IBudgetService
{
    List<Budget> GetBudgets(int month, int year);
    decimal GetTotalBudget(int month, int year);
    decimal GetTotalSpent(int month, int year);
    decimal GetSpentByCategory(int categoryId, int month, int year);
    decimal GetBudgetByCategory(int categoryId, int month, int year);
    bool IsBudgetCustom(int categoryId, int month, int year);
    void UpdateBudget(int categoryId, int month, int year, decimal amount);
    void ResetBudgetToDefault(int categoryId, int month, int year);
}
