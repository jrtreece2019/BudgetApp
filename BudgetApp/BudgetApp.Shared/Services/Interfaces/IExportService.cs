using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Generates CSV export content from budget data.
/// The actual file download is triggered via JS interop in the page.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Generates a CSV string from a list of transactions.
    /// Includes headers: Date, Description, Category, Type, Amount.
    /// </summary>
    string GenerateTransactionsCsv(List<Transaction> transactions, List<Category> categories);

    /// <summary>
    /// Generates a CSV string with a monthly budget report.
    /// Includes: Category, Type, Budget, Spent, Remaining.
    /// </summary>
    string GenerateBudgetReportCsv(
        List<Category> categories,
        Func<int, decimal> getBudget,
        Func<int, decimal> getSpent);
}
