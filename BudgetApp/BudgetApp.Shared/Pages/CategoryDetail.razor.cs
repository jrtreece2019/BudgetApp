using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class CategoryDetail : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ITransactionService TransactionService { get; set; } = default!;
    [Inject] private IBudgetService BudgetService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int CategoryId { get; set; }

    [Parameter]
    public int? Month { get; set; }

    [Parameter]
    public int? Year { get; set; }

    private Category? Category { get; set; }
    private List<Transaction> Transactions { get; set; } = new();
    private DateTime CurrentDate { get; set; } = DateTime.Now;
    private decimal TotalSpent { get; set; }
    private decimal Budget { get; set; }
    private bool IsBudgetCustom { get; set; }
    private decimal Remaining => Budget - TotalSpent;
    private double ProgressPercentage => Budget > 0 ? Math.Min((double)(TotalSpent / Budget) * 100, 100) : 0;

    private bool IsEditingBudget { get; set; }
    private decimal EditBudgetAmount { get; set; }

    protected override void OnInitialized()
    {
        if (Month.HasValue && Year.HasValue)
        {
            CurrentDate = new DateTime(Year.Value, Month.Value, 1);
        }
        LoadData();
    }

    protected override void OnParametersSet()
    {
        if (Month.HasValue && Year.HasValue)
        {
            CurrentDate = new DateTime(Year.Value, Month.Value, 1);
        }
        LoadData();
    }

    private void LoadData()
    {
        var categories = CategoryService.GetCategories();
        Category = categories.FirstOrDefault(c => c.Id == CategoryId);

        if (Category != null)
        {
            var allTransactions = TransactionService.GetTransactions(CurrentDate.Month, CurrentDate.Year);
            Transactions = allTransactions
                .Where(t => t.CategoryId == CategoryId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToList();

            TotalSpent = BudgetService.GetSpentByCategory(CategoryId, CurrentDate.Month, CurrentDate.Year);
            Budget = BudgetService.GetBudgetByCategory(CategoryId, CurrentDate.Month, CurrentDate.Year);
            IsBudgetCustom = BudgetService.IsBudgetCustom(CategoryId, CurrentDate.Month, CurrentDate.Year);
        }
    }

    private void GoBack()
    {
        Navigation.NavigateTo($"/?month={CurrentDate.Month}&year={CurrentDate.Year}");
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

    private void AddTransaction()
    {
        Navigation.NavigateTo("/add");
    }

    private void DeleteTransaction(int transactionId)
    {
        TransactionService.DeleteTransaction(transactionId);
        LoadData();
    }

    private void EditTransaction(int transactionId)
    {
        Navigation.NavigateTo($"/edit/{transactionId}");
    }

    private void StartEditingBudget()
    {
        if (!IsEditingBudget)
        {
            EditBudgetAmount = Budget;
            IsEditingBudget = true;
        }
    }

    private void SaveBudget()
    {
        if (EditBudgetAmount >= 0 && Category != null)
        {
            BudgetService.UpdateBudget(CategoryId, CurrentDate.Month, CurrentDate.Year, EditBudgetAmount);
            LoadData();
        }
        IsEditingBudget = false;
    }

    private void CancelEditBudget()
    {
        IsEditingBudget = false;
    }

    private void ResetBudgetToDefault()
    {
        BudgetService.ResetBudgetToDefault(CategoryId, CurrentDate.Month, CurrentDate.Year);
        LoadData();
        IsEditingBudget = false;
    }

    private string AdjustColor(string hexColor)
    {
        if (hexColor.StartsWith("#") && hexColor.Length == 7)
        {
            try
            {
                int r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
                int g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
                int b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

                r = Math.Max(0, r - 40);
                g = Math.Max(0, g - 40);
                b = Math.Max(0, b - 40);

                return $"#{r:X2}{g:X2}{b:X2}";
            }
            catch
            {
                return hexColor;
            }
        }
        return hexColor;
    }
}
