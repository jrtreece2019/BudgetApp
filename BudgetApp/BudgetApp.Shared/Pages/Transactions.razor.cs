using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class Transactions : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ITransactionService TransactionService { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private DateTime CurrentDate { get; set; } = DateTime.Now;
    private List<Category> Categories { get; set; } = new();
    private List<Transaction> AllTransactions { get; set; } = new();

    private string SearchQuery { get; set; } = string.Empty;
    private int SelectedCategoryId { get; set; }

    private List<Transaction> FilteredTransactions => AllTransactions
        .Where(t => string.IsNullOrEmpty(SearchQuery) ||
                    t.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
        .Where(t => SelectedCategoryId == 0 || // All categories
                    (SelectedCategoryId == -1 && t.Type == TransactionType.Income) || // Income filter
                    t.CategoryId == SelectedCategoryId) // Specific category
        .OrderByDescending(t => t.Date)
        .ThenByDescending(t => t.Id)
        .ToList();

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

    private void DeleteTransaction(int transactionId)
    {
        TransactionService.DeleteTransaction(transactionId);
        LoadData();
    }

    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    private void ClearFilters()
    {
        SearchQuery = string.Empty;
        SelectedCategoryId = 0;
    }
}
