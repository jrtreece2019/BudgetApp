using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class EditRecurring : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ITransactionService TransactionService { get; set; } = default!;
    [Inject] private IRecurringTransactionService RecurringTransactionService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int? RecurringId { get; set; }

    private bool IsEditing => RecurringId.HasValue;
    private bool IsExpense { get; set; } = true;
    private decimal Amount { get; set; }
    private string Description { get; set; } = string.Empty;
    private int SelectedCategoryId { get; set; }
    private RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;
    private int DayOfMonth { get; set; } = DateTime.Today.Day;
    private DateTime StartDate { get; set; } = DateTime.Today;
    private bool IsActive { get; set; } = true;
    private List<Category> Categories { get; set; } = new();

    private bool IsValid => Amount > 0 && (IsExpense ? SelectedCategoryId > 0 : true) && !string.IsNullOrWhiteSpace(Description);

    protected override void OnInitialized()
    {
        Categories = CategoryService.GetCategories();

        if (IsEditing)
        {
            var recurring = RecurringTransactionService.GetRecurringTransaction(RecurringId!.Value);
            if (recurring != null)
            {
                Amount = recurring.Amount;
                Description = recurring.Description;
                SelectedCategoryId = recurring.CategoryId;
                IsExpense = recurring.Type == TransactionType.Expense;
                Frequency = recurring.Frequency;
                DayOfMonth = recurring.DayOfMonth;
                StartDate = recurring.StartDate;
                IsActive = recurring.IsActive;
            }
        }
        else if (Categories.Any())
        {
            SelectedCategoryId = Categories.First().Id;
        }
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/recurring");
    }

    private void Save()
    {
        if (!IsValid) return;

        var nextDueDate = CalculateInitialNextDueDate();

        var categoryId = IsExpense ? SelectedCategoryId : 0;
        var transactionType = IsExpense ? TransactionType.Expense : TransactionType.Income;

        if (IsEditing)
        {
            var recurring = new RecurringTransaction
            {
                Id = RecurringId!.Value,
                Description = Description,
                Amount = Amount,
                CategoryId = categoryId,
                Type = transactionType,
                Frequency = Frequency,
                DayOfMonth = DayOfMonth,
                StartDate = StartDate,
                NextDueDate = nextDueDate,
                IsActive = IsActive
            };
            RecurringTransactionService.UpdateRecurringTransaction(recurring);
        }
        else
        {
            var firstTransaction = new Transaction
            {
                Description = Description,
                Amount = Amount,
                CategoryId = categoryId,
                Type = transactionType,
                Date = DateTime.Today
            };
            TransactionService.AddTransaction(firstTransaction);

            var recurring = new RecurringTransaction
            {
                Description = Description,
                Amount = Amount,
                CategoryId = categoryId,
                Type = transactionType,
                Frequency = Frequency,
                DayOfMonth = DayOfMonth,
                StartDate = StartDate,
                NextDueDate = nextDueDate,
                IsActive = true
            };
            RecurringTransactionService.AddRecurringTransaction(recurring);
        }

        Navigation.NavigateTo("/");
    }

    private DateTime CalculateInitialNextDueDate()
    {
        var today = DateTime.Today;

        if (Frequency == RecurrenceFrequency.Monthly)
        {
            var nextMonth = today.AddMonths(1);
            var daysInNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
            var day = Math.Min(DayOfMonth, daysInNextMonth);
            return new DateTime(nextMonth.Year, nextMonth.Month, day);
        }

        return Frequency switch
        {
            RecurrenceFrequency.Weekly => today.AddDays(7),
            RecurrenceFrequency.Biweekly => today.AddDays(14),
            RecurrenceFrequency.Yearly => today.AddYears(1),
            _ => today.AddMonths(1)
        };
    }

    // Confirm delete state
    private bool ShowDeleteConfirm { get; set; }

    private void ConfirmDelete()
    {
        ShowDeleteConfirm = true;
    }

    private void CancelDelete()
    {
        ShowDeleteConfirm = false;
    }

    private void DeleteRecurring()
    {
        if (IsEditing)
        {
            RecurringTransactionService.DeleteRecurringTransaction(RecurringId!.Value);
            ShowDeleteConfirm = false;
            Navigation.NavigateTo("/recurring");
        }
    }
}
