using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class Reports : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ITransactionService TransactionService { get; set; } = default!;
    [Inject] private IBudgetService BudgetService { get; set; } = default!;
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

    private class CategorySpendingItem
    {
        public Category Category { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
