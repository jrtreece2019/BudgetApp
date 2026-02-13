using System.Text;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// Generates CSV content for exporting budget data.
/// 
/// CSV (Comma-Separated Values) is a simple text format that spreadsheet apps
/// like Excel and Google Sheets can open. Each line is a row, columns are
/// separated by commas, and values containing commas are wrapped in quotes.
/// </summary>
public class ExportService : IExportService
{
    public string GenerateTransactionsCsv(List<Transaction> transactions, List<Category> categories)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Date,Description,Category,Type,Amount");

        foreach (var t in transactions.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id))
        {
            var categoryName = t.Type == TransactionType.Income
                ? "Income"
                : categories.FirstOrDefault(c => c.Id == t.CategoryId)?.Name ?? "Unknown";

            var typeName = t.Type == TransactionType.Income ? "Income" : "Expense";

            // Wrap description in quotes in case it contains commas.
            sb.AppendLine($"{t.Date:yyyy-MM-dd},{EscapeCsv(t.Description)},{EscapeCsv(categoryName)},{typeName},{t.Amount:F2}");
        }

        return sb.ToString();
    }

    public string GenerateBudgetReportCsv(
        List<Category> categories,
        Func<int, decimal> getBudget,
        Func<int, decimal> getSpent)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Category,Type,Budget,Spent,Remaining,Percentage Used");

        foreach (var c in categories)
        {
            var budget = getBudget(c.Id);
            var spent = getSpent(c.Id);
            var remaining = budget - spent;
            var percentage = budget > 0 ? (spent / budget * 100) : 0;

            var typeName = c.Type switch
            {
                CategoryType.Fixed => "Fixed",
                CategoryType.Discretionary => "Discretionary",
                CategoryType.Savings => "Savings",
                _ => "Other"
            };

            sb.AppendLine($"{EscapeCsv(c.Name)},{typeName},{budget:F2},{spent:F2},{remaining:F2},{percentage:F0}%");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a CSV field by wrapping in quotes if it contains commas,
    /// quotes, or newlines. Any embedded quotes are doubled.
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
