using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class Reports : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ITransactionService TransactionService { get; set; } = default!;
    [Inject] private IBudgetService BudgetService { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private IExportService ExportService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private DateTime CurrentDate { get; set; } = DateTime.Now;
    private List<Category> Categories { get; set; } = new();
    private List<Transaction> Transactions { get; set; } = new();
    private List<CategorySpendingItem> CategorySpending { get; set; } = new();

    private decimal TotalExpenses => Transactions
        .Where(t => t.Type == TransactionType.Expense)
        .Sum(t => t.Amount);

    private decimal TotalIncome => Transactions
        .Where(t => t.Type == TransactionType.Income)
        .Sum(t => t.Amount);

    private decimal NetAmount => TotalIncome - TotalExpenses;

    // Category deep-dive state
    private int SelectedDeepDiveCategoryId { get; set; }
    private List<CategoryMonthItem> CategoryHistory { get; set; } = new();
    private decimal CategoryHistoryAvg => CategoryHistory.Count > 0 ? CategoryHistory.Average(h => h.Amount) : 0;
    private decimal CategoryHistoryMax => CategoryHistory.Count > 0 ? CategoryHistory.Max(h => h.Amount) : 1;

    protected override void OnInitialized()
    {
        LoadData();
    }

    private void LoadData()
    {
        Categories = CategoryService.GetCategories();
        Transactions = TransactionService.GetTransactions(CurrentDate.Month, CurrentDate.Year);

        CategorySpending = Categories
            .Select(c => new CategorySpendingItem
            {
                Category = c,
                Amount = Transactions
                    .Where(t => t.CategoryId == c.Id && t.Type == TransactionType.Expense)
                    .Sum(t => t.Amount)
            })
            .Where(c => c.Amount > 0)
            .ToList();
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }

    private void PreviousMonth()
    {
        CurrentDate = CurrentDate.AddMonths(-1);
        LoadData();
    }

    private void NextMonth()
    {
        CurrentDate = CurrentDate.AddMonths(1);
        LoadData();
    }

    /// <summary>
    /// Called when the user selects a category from the deep-dive dropdown.
    /// Loads up to 6 months of spending history for that category, but only
    /// includes months where the category actually had transactions.
    /// This avoids the "divide by 6 with only 1 month of data" problem.
    /// </summary>
    private void OnDeepDiveCategoryChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var catId) && catId > 0)
        {
            SelectedDeepDiveCategoryId = catId;
            LoadCategoryHistory(catId);
        }
        else
        {
            SelectedDeepDiveCategoryId = 0;
            CategoryHistory.Clear();
        }
    }

    private void LoadCategoryHistory(int categoryId)
    {
        CategoryHistory = new List<CategoryMonthItem>();

        // Walk backwards from the current month, collecting months with data,
        // up to a maximum of 6 data points and looking back at most 12 months.
        for (int i = 0; i < 12 && CategoryHistory.Count < 6; i++)
        {
            var date = CurrentDate.AddMonths(-i);
            var transactions = TransactionService.GetTransactions(date.Month, date.Year);
            var amount = transactions
                .Where(t => t.CategoryId == categoryId && t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            // Only include months where this category had actual spending.
            if (amount > 0)
            {
                CategoryHistory.Add(new CategoryMonthItem
                {
                    Month = date,
                    Label = date.ToString("MMM yyyy"),
                    Amount = amount
                });
            }
        }

        // Reverse so oldest is first, newest is last.
        CategoryHistory.Reverse();
    }

    private double GetDeepDiveBarWidth(decimal value)
    {
        if (CategoryHistoryMax <= 0) return 0;
        return (double)(value / CategoryHistoryMax) * 100;
    }

    private async Task ExportReport()
    {
        var csv = ExportService.GenerateBudgetReportCsv(
            Categories,
            catId => BudgetService.GetBudgetByCategory(catId, CurrentDate.Month, CurrentDate.Year),
            catId => BudgetService.GetSpentByCategory(catId, CurrentDate.Month, CurrentDate.Year));

        var filename = $"budget-report-{CurrentDate:yyyy-MM}.csv";

        try
        {
            await JSRuntime.InvokeVoidAsync("fileDownload.downloadCsv", filename, csv);
        }
        catch
        {
            // JS interop not available
        }
    }

    private class CategorySpendingItem
    {
        public Category Category { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    private class CategoryMonthItem
    {
        public DateTime Month { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
