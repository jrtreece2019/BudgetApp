using System.Web;
using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class AddTransaction : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ITransactionService TransactionService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int? TransactionId { get; set; }

    private bool IsEditing => TransactionId.HasValue;
    private bool IsExpense { get; set; } = true;
    private decimal Amount { get; set; }
    private string Description { get; set; } = string.Empty;
    private int SelectedCategoryId { get; set; }
    private DateTime TransactionDate { get; set; } = DateTime.Today;
    private List<Category> Categories { get; set; } = new();
    private string? ReturnUrl { get; set; }

    private bool IsValid => Amount > 0 && (IsExpense ? SelectedCategoryId > 0 : !string.IsNullOrWhiteSpace(Description));

    protected override void OnInitialized()
    {
        Categories = CategoryService.GetCategories();

        var uri = new Uri(Navigation.Uri);
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var queryParams = HttpUtility.ParseQueryString(uri.Query);
            var returnUrlParam = queryParams["returnUrl"];
            if (!string.IsNullOrEmpty(returnUrlParam))
            {
                ReturnUrl = returnUrlParam;
            }

            var categoryIdParam = queryParams["categoryId"];
            if (int.TryParse(categoryIdParam, out var catId) && catId > 0)
            {
                SelectedCategoryId = catId;
            }
        }

        if (IsEditing)
        {
            var transaction = TransactionService.GetTransaction(TransactionId!.Value);
            if (transaction != null)
            {
                Amount = transaction.Amount;
                Description = transaction.Description;
                SelectedCategoryId = transaction.CategoryId;
                TransactionDate = transaction.Date;
                IsExpense = transaction.Type == TransactionType.Expense;

                if (string.IsNullOrEmpty(ReturnUrl))
                {
                    ReturnUrl = transaction.CategoryId > 0
                        ? $"/category/{transaction.CategoryId}"
                        : "/";
                }
            }
        }
        else if (SelectedCategoryId == 0 && Categories.Any())
        {
            SelectedCategoryId = Categories.First().Id;
        }
    }

    private void GoBack()
    {
        Navigation.NavigateTo(ReturnUrl ?? "/");
    }

    private void SaveTransaction()
    {
        if (!IsValid) return;

        var description = string.IsNullOrWhiteSpace(Description) && IsExpense
            ? Categories.FirstOrDefault(c => c.Id == SelectedCategoryId)?.Name ?? "Expense"
            : Description;

        if (IsEditing)
        {
            var transaction = new Transaction
            {
                Id = TransactionId!.Value,
                Description = description,
                Amount = Amount,
                Date = TransactionDate,
                CategoryId = IsExpense ? SelectedCategoryId : 0,
                Type = IsExpense ? TransactionType.Expense : TransactionType.Income
            };
            TransactionService.UpdateTransaction(transaction);
        }
        else
        {
            var transaction = new Transaction
            {
                Description = description,
                Amount = Amount,
                Date = TransactionDate,
                CategoryId = IsExpense ? SelectedCategoryId : 0,
                Type = IsExpense ? TransactionType.Expense : TransactionType.Income
            };
            TransactionService.AddTransaction(transaction);
        }

        Navigation.NavigateTo(ReturnUrl ?? "/");
    }
}
