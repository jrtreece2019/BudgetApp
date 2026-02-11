using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// SQLite-backed implementation of IBudgetService.
/// Handles the "custom budget vs. default budget" fallback logic.
/// </summary>
public class SqliteBudgetService : IBudgetService
{
    private readonly IDatabaseService _db;

    public SqliteBudgetService(IDatabaseService db)
    {
        _db = db;
    }

    public List<Budget> GetBudgets(int month, int year)
        => _db.GetBudgets(month, year);

    /// <summary>
    /// Sums the effective budget for every category — uses a custom monthly
    /// override when one exists, otherwise falls back to the category default.
    /// </summary>
    public decimal GetTotalBudget(int month, int year)
    {
        var categories = _db.GetCategories();
        decimal total = 0;
        foreach (var category in categories)
            total += GetBudgetByCategory(category.Id, month, year);
        return total;
    }

    public decimal GetTotalSpent(int month, int year)
        => _db.GetTransactions(month, year)
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

    public decimal GetSpentByCategory(int categoryId, int month, int year)
        => _db.GetTransactions(month, year)
            .Where(t => t.CategoryId == categoryId && t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

    /// <summary>
    /// Returns the custom budget for the month if set, otherwise the
    /// category's default budget.
    /// </summary>
    public decimal GetBudgetByCategory(int categoryId, int month, int year)
    {
        var budget = _db.GetBudget(categoryId, month, year);
        if (budget != null)
            return budget.Amount;

        var category = _db.GetCategory(categoryId);
        return category?.DefaultBudget ?? 0;
    }

    public bool IsBudgetCustom(int categoryId, int month, int year)
        => _db.GetBudget(categoryId, month, year) != null;

    public void UpdateBudget(int categoryId, int month, int year, decimal amount)
        => _db.UpsertBudget(categoryId, month, year, amount);

    /// <summary>
    /// Removes the month-specific override so the category falls back to its default.
    /// </summary>
    public void ResetBudgetToDefault(int categoryId, int month, int year)
        => _db.DeleteBudget(categoryId, month, year);
}
