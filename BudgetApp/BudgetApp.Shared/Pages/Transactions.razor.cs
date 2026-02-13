using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class Transactions : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ITransactionService TransactionService { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private IExportService ExportService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private DateTime CurrentDate { get; set; } = DateTime.Now;
    private List<Category> Categories { get; set; } = new();
    private List<Transaction> AllTransactions { get; set; } = new();

    private string SearchQuery { get; set; } = string.Empty;
    private int SelectedCategoryId { get; set; }
    private bool SearchAllMonths { get; set; }
    private List<Transaction> GlobalSearchResults { get; set; } = new();

    private List<Transaction> FilteredTransactions
    {
        get
        {
            if (SearchAllMonths && !string.IsNullOrWhiteSpace(SearchQuery))
            {
                return GlobalSearchResults;
            }

            return AllTransactions
                .Where(t => string.IsNullOrEmpty(SearchQuery) ||
                            t.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                .Where(t => SelectedCategoryId == 0 ||
                            (SelectedCategoryId == -1 && t.Type == TransactionType.Income) ||
                            t.CategoryId == SelectedCategoryId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToList();
        }
    }

    private decimal TotalExpenses => FilteredTransactions
        .Where(t => t.Type == TransactionType.Expense)
        .Sum(t => t.Amount);

    private decimal ExpectedIncome { get; set; }
    private decimal ActualIncome { get; set; }
    private int PaycheckCount { get; set; }

    private decimal NetAmount => ExpectedIncome - TotalExpenses;

    protected override void OnInitialized()
    {
        LoadData();
    }

    private void LoadData()
    {
        Categories = CategoryService.GetCategories();
        AllTransactions = TransactionService.GetTransactions(CurrentDate.Month, CurrentDate.Year);

        ExpectedIncome = SettingsService.GetMonthlyIncome();

        var paychecks = AllTransactions.Where(t => t.Type == TransactionType.Income).ToList();
        ActualIncome = paychecks.Sum(t => t.Amount);
        PaycheckCount = paychecks.Count;
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

    private void EditTransaction(int transactionId)
    {
        Navigation.NavigateTo($"/edit/{transactionId}?returnUrl=/transactions");
    }

    private void AddTransaction()
    {
        Navigation.NavigateTo("/add?returnUrl=/transactions");
    }

    // Confirm delete state
    private bool ShowDeleteConfirm { get; set; }
    private int? PendingDeleteTransactionId { get; set; }

    private void ConfirmDeleteTransaction(int transactionId)
    {
        PendingDeleteTransactionId = transactionId;
        ShowDeleteConfirm = true;
    }

    private void CancelDelete()
    {
        ShowDeleteConfirm = false;
        PendingDeleteTransactionId = null;
    }

    private void DeleteTransaction()
    {
        if (PendingDeleteTransactionId.HasValue)
        {
            TransactionService.DeleteTransaction(PendingDeleteTransactionId.Value);
            LoadData();
        }
        ShowDeleteConfirm = false;
        PendingDeleteTransactionId = null;
    }

    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        GlobalSearchResults.Clear();
    }

    private void ClearFilters()
    {
        SearchQuery = string.Empty;
        SelectedCategoryId = 0;
        SearchAllMonths = false;
        GlobalSearchResults.Clear();
    }

    private void ToggleSearchAllMonths()
    {
        SearchAllMonths = !SearchAllMonths;
        if (SearchAllMonths && !string.IsNullOrWhiteSpace(SearchQuery))
        {
            RunGlobalSearch();
        }
        else
        {
            GlobalSearchResults.Clear();
        }
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        SearchQuery = e.Value?.ToString() ?? string.Empty;
        if (SearchAllMonths && !string.IsNullOrWhiteSpace(SearchQuery))
        {
            RunGlobalSearch();
        }
    }

    private void RunGlobalSearch()
    {
        var catId = SelectedCategoryId == 0 ? (int?)null : SelectedCategoryId;
        GlobalSearchResults = TransactionService.SearchTransactions(SearchQuery, catId);
    }

    private async Task ExportTransactions()
    {
        var csv = ExportService.GenerateTransactionsCsv(FilteredTransactions, Categories);
        var filename = $"transactions-{CurrentDate:yyyy-MM}.csv";

        try
        {
            await JSRuntime.InvokeVoidAsync("fileDownload.downloadCsv", filename, csv);
        }
        catch
        {
            // JS interop not available
        }
    }
}
