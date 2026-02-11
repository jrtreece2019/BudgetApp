using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class RecurringTransactions : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private IRecurringTransactionService RecurringTransactionService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<RecurringTransaction> RecurringList { get; set; } = new();
    private List<Category> Categories { get; set; } = new();

    protected override void OnInitialized()
    {
        LoadData();
    }

    private void LoadData()
    {
        Categories = CategoryService.GetCategories();
        RecurringList = RecurringTransactionService.GetRecurringTransactions()
            .OrderBy(r => r.NextDueDate)
            .ToList();
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }

    private void AddRecurring()
    {
        Navigation.NavigateTo("/recurring/add");
    }

    private void EditRecurring(int id)
    {
        Navigation.NavigateTo($"/recurring/edit/{id}");
    }

    private string GetFrequencyText(RecurrenceFrequency frequency)
    {
        return frequency switch
        {
            RecurrenceFrequency.Weekly => "Weekly",
            RecurrenceFrequency.Biweekly => "Every 2 weeks",
            RecurrenceFrequency.Monthly => "Monthly",
            RecurrenceFrequency.Yearly => "Yearly",
            _ => "Monthly"
        };
    }
}
