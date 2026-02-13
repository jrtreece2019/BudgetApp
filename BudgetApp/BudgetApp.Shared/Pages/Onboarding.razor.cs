using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class Onboarding : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private int CurrentStep { get; set; } = 1;
    private decimal MonthlyIncome { get; set; }

    private List<Category> AllCategories { get; set; } = new();

    // Tracks which categories the user wants to keep (checked = true).
    private Dictionary<int, bool> SelectedCategories { get; set; } = new();

    // Tracks editable budget amounts per category.
    private Dictionary<int, decimal> CategoryBudgets { get; set; } = new();

    // Group categories by type for display
    private List<CategoryGroupViewModel> CategoryGroups => new()
    {
        new("Fixed Expenses", AllCategories.Where(c => c.Type == CategoryType.Fixed).ToList()),
        new("Discretionary", AllCategories.Where(c => c.Type == CategoryType.Discretionary).ToList()),
        new("Savings & Investments", AllCategories.Where(c => c.Type == CategoryType.Savings).ToList())
    };

    // Only count selected categories towards budget totals.
    private decimal TotalBudgeted => AllCategories
        .Where(c => SelectedCategories.GetValueOrDefault(c.Id, true))
        .Sum(c => CategoryBudgets.GetValueOrDefault(c.Id, c.DefaultBudget));

    private decimal Remaining => MonthlyIncome - TotalBudgeted;

    protected override void OnInitialized()
    {
        // Load existing categories (the default ones seeded by DatabaseService).
        AllCategories = CategoryService.GetCategories();
        MonthlyIncome = SettingsService.GetMonthlyIncome();

        // Initialise selection & budget dictionaries from the defaults.
        foreach (var cat in AllCategories)
        {
            SelectedCategories[cat.Id] = true;
            CategoryBudgets[cat.Id] = cat.DefaultBudget;
        }
    }

    private void ToggleCategory(int categoryId)
    {
        SelectedCategories[categoryId] = !SelectedCategories.GetValueOrDefault(categoryId, true);
    }

    private void OnBudgetChanged(int categoryId, decimal value)
    {
        CategoryBudgets[categoryId] = value;
    }

    private bool IsCategorySelected(int categoryId)
        => SelectedCategories.GetValueOrDefault(categoryId, true);

    private decimal GetCategoryBudget(int categoryId)
        => CategoryBudgets.GetValueOrDefault(categoryId, 0);

    private void NextStep()
    {
        if (CurrentStep == 1)
        {
            // Save income before moving forward.
            if (MonthlyIncome > 0)
            {
                SettingsService.SetMonthlyIncome(MonthlyIncome);
            }
        }

        CurrentStep++;
    }

    private void PreviousStep()
    {
        if (CurrentStep > 1) CurrentStep--;
    }

    private void SkipOnboarding()
    {
        // Mark onboarding complete even when skipping, so the user isn't
        // prompted again on the next visit.
        SettingsService.SetOnboardingComplete();
        Navigation.NavigateTo("/");
    }

    /// <summary>
    /// Skip categories step -- user can set them up later from the
    /// Categories page.  Deletes all default categories so the Home page
    /// can show a helpful tip instead of empty budget rows.
    /// </summary>
    private void SkipCategories()
    {
        // Delete all default categories since the user chose to skip.
        foreach (var cat in AllCategories)
        {
            CategoryService.DeleteCategory(cat.Id);
        }

        CurrentStep++;
    }

    private void FinishOnboarding()
    {
        // Persist the user's category choices:
        // - Delete deselected categories.
        // - Update budgets for any selected category where the amount changed.
        foreach (var cat in AllCategories)
        {
            if (!SelectedCategories.GetValueOrDefault(cat.Id, true))
            {
                CategoryService.DeleteCategory(cat.Id);
            }
            else
            {
                var newBudget = CategoryBudgets.GetValueOrDefault(cat.Id, cat.DefaultBudget);
                if (newBudget != cat.DefaultBudget)
                {
                    cat.DefaultBudget = newBudget;
                    CategoryService.UpdateCategory(cat);
                }
            }
        }

        // Save income one more time in case it was changed.
        if (MonthlyIncome > 0)
        {
            SettingsService.SetMonthlyIncome(MonthlyIncome);
        }

        SettingsService.SetOnboardingComplete();
        Navigation.NavigateTo("/");
    }

    /// <summary>
    /// Simple view model for grouping categories by type in the template.
    /// </summary>
    private record CategoryGroupViewModel(string Title, List<Category> Categories);
}
