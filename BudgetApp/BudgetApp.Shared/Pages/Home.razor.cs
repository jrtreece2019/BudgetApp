using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Helpers;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class Home : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private ITransactionService TransactionService { get; set; } = default!;
    [Inject] private IBudgetService BudgetService { get; set; } = default!;
    [Inject] private IRecurringTransactionService RecurringTransactionService { get; set; } = default!;
    [Inject] private ISettingsService SettingsService { get; set; } = default!;
    [Inject] private ISinkingFundService SinkingFundService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private DateTime CurrentDate { get; set; } = DateTime.Now;
    private List<Category> Categories { get; set; } = new();
    private List<Transaction> RecentTransactions { get; set; } = new();

    // Expected vs Actual Income
    private decimal ExpectedIncome { get; set; }  // From setting - used for budget calculations
    private decimal ActualIncome { get; set; }    // Sum of received paychecks
    private decimal Unallocated => ExpectedIncome - TotalBudget;
    private decimal RemainingIncome => ExpectedIncome - TotalSpent;
    private double ProgressPercentage => ExpectedIncome > 0 ? Math.Min((double)(TotalSpent / ExpectedIncome) * 100, 100) : 0;

    // Budget totals
    private decimal TotalBudget { get; set; }
    private decimal TotalSpent { get; set; }

    // Income editing
    private bool IsEditingExpectedIncome { get; set; }
    private decimal EditExpectedIncomeAmount { get; set; }

    // Paycheck modal
    private bool ShowPaycheckModal { get; set; }
    private string PaycheckDescription { get; set; } = string.Empty;
    private decimal PaycheckAmount { get; set; }
    private DateTime PaycheckDate { get; set; } = DateTime.Today;
    private bool IsRecurringPaycheck { get; set; }
    private RecurrenceFrequency PaycheckFrequency { get; set; } = RecurrenceFrequency.Biweekly;
    private int PaycheckDayOfMonth { get; set; } = 1;
    private bool IsPaycheckValid => PaycheckAmount > 0 && !string.IsNullOrWhiteSpace(PaycheckDescription);
    private int IncomeTransactionCount { get; set; }
    private List<Transaction> RecentPaychecks { get; set; } = new();
    private List<RecurringTransaction> RecurringPaychecks { get; set; } = new();

    // Grouped categories
    private List<Category> FixedCategories => Categories.Where(c => c.Type == CategoryType.Fixed).ToList();
    private List<Category> DiscretionaryCategories => Categories.Where(c => c.Type == CategoryType.Discretionary).ToList();
    private List<Category> SavingsCategories => Categories.Where(c => c.Type == CategoryType.Savings).ToList();

    // Section expand state
    private bool FixedExpanded { get; set; } = true;
    private bool DiscretionaryExpanded { get; set; } = true;
    private bool SavingsExpanded { get; set; } = true;

    // Group totals
    private decimal FixedBudget { get; set; }
    private decimal FixedSpent { get; set; }
    private decimal DiscretionaryBudget { get; set; }
    private decimal DiscretionarySpent { get; set; }
    private decimal SavingsBudget { get; set; }
    private decimal SavingsSpent { get; set; }

    // Sinking Funds
    private List<SinkingFund> SinkingFunds { get; set; } = new();
    private bool SinkingFundsExpanded { get; set; } = true;
    private IEnumerable<SinkingFund> ActiveFunds => SinkingFunds.Where(f => f.Status == SinkingFundStatus.Active);
    private decimal TotalGoal => SinkingFunds.Sum(f => f.GoalAmount);
    private decimal TotalSaved => SinkingFunds.Sum(f => f.CurrentBalance);

    // Flag to defer navigation until after the first render.  Calling
    // NavigateTo() inside OnInitialized() on Blazor Server throws a
    // NavigationException because the component hasn't finished rendering yet.
    private bool _redirectToOnboarding;

    protected override void OnInitialized()
    {
        // Check for month/year query parameters (when returning from category detail)
        var uri = new Uri(Navigation.Uri);
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (int.TryParse(queryParams["month"], out var month) &&
                int.TryParse(queryParams["year"], out var year))
            {
                CurrentDate = new DateTime(year, month, 1);
            }
        }

        // First-run detection: redirect to the onboarding wizard if the user
        // hasn't completed (or skipped) it yet.  Uses an explicit database flag
        // instead of the old "income == 0" heuristic, so users who skip setup
        // or genuinely have $0 income aren't stuck in a loop.
        if (!SettingsService.HasCompletedOnboarding())
        {
            // Don't call NavigateTo here -- on Blazor Server it throws a
            // NavigationException.  Set a flag and redirect in OnAfterRenderAsync.
            _redirectToOnboarding = true;
            return;
        }

        // Process any pending recurring transactions
        RecurringTransactionService.ProcessRecurringTransactions();

        LoadData();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _redirectToOnboarding)
        {
            Navigation.NavigateTo("/onboarding");
        }
    }

    private void LoadData()
    {
        Categories = CategoryService.GetCategories();
        RecentTransactions = TransactionService.GetTransactions(CurrentDate.Month, CurrentDate.Year);

        // Expected income from setting (for budget calculations)
        ExpectedIncome = SettingsService.GetMonthlyIncome();

        // Actual income from received paychecks
        RecentPaychecks = RecentTransactions
            .Where(t => t.Type == TransactionType.Income)
            .OrderByDescending(t => t.Date)
            .ToList();
        ActualIncome = RecentPaychecks.Sum(t => t.Amount);
        IncomeTransactionCount = RecentPaychecks.Count;

        // Get recurring paychecks (income type recurring transactions)
        RecurringPaychecks = RecurringTransactionService.GetRecurringTransactions()
            .Where(r => r.Type == TransactionType.Income && r.IsActive)
            .ToList();

        TotalBudget = BudgetService.GetTotalBudget(CurrentDate.Month, CurrentDate.Year);
        TotalSpent = BudgetService.GetTotalSpent(CurrentDate.Month, CurrentDate.Year);

        // Calculate group totals
        FixedBudget = FixedCategories.Sum(c => BudgetService.GetBudgetByCategory(c.Id, CurrentDate.Month, CurrentDate.Year));
        FixedSpent = FixedCategories.Sum(c => BudgetService.GetSpentByCategory(c.Id, CurrentDate.Month, CurrentDate.Year));
        DiscretionaryBudget = DiscretionaryCategories.Sum(c => BudgetService.GetBudgetByCategory(c.Id, CurrentDate.Month, CurrentDate.Year));
        DiscretionarySpent = DiscretionaryCategories.Sum(c => BudgetService.GetSpentByCategory(c.Id, CurrentDate.Month, CurrentDate.Year));
        SavingsBudget = SavingsCategories.Sum(c => BudgetService.GetBudgetByCategory(c.Id, CurrentDate.Month, CurrentDate.Year));
        SavingsSpent = SavingsCategories.Sum(c => BudgetService.GetSpentByCategory(c.Id, CurrentDate.Month, CurrentDate.Year));

        // Load sinking funds
        SinkingFunds = SinkingFundService.GetSinkingFunds();
    }

    // Paycheck modal methods
    private void OpenIncomeModal()
    {
        PaycheckDescription = string.Empty;
        PaycheckAmount = 0;
        PaycheckDate = DateTime.Today;
        IsRecurringPaycheck = false;
        PaycheckFrequency = RecurrenceFrequency.Biweekly;
        PaycheckDayOfMonth = 15;
        ShowPaycheckModal = true;
    }

    private void ClosePaycheckModal()
    {
        ShowPaycheckModal = false;
    }

    private void SavePaycheck()
    {
        if (!IsPaycheckValid) return;

        if (IsRecurringPaycheck)
        {
            // Create a recurring paycheck
            var recurring = new RecurringTransaction
            {
                Description = PaycheckDescription,
                Amount = PaycheckAmount,
                CategoryId = 0,
                Type = TransactionType.Income,
                Frequency = PaycheckFrequency,
                DayOfMonth = PaycheckFrequency == RecurrenceFrequency.Monthly ? PaycheckDayOfMonth : PaycheckDate.Day,
                StartDate = PaycheckFrequency == RecurrenceFrequency.Monthly
                    ? new DateTime(CurrentDate.Year, CurrentDate.Month, Math.Min(PaycheckDayOfMonth, DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month)))
                    : PaycheckDate,
                NextDueDate = PaycheckFrequency == RecurrenceFrequency.Monthly
                    ? new DateTime(CurrentDate.Year, CurrentDate.Month, Math.Min(PaycheckDayOfMonth, DateTime.DaysInMonth(CurrentDate.Year, CurrentDate.Month)))
                    : PaycheckDate,
                IsActive = true
            };
            RecurringTransactionService.AddRecurringTransaction(recurring);

            // Also create the first transaction immediately if the date is today or earlier
            if (recurring.NextDueDate <= DateTime.Today)
            {
                var transaction = new Transaction
                {
                    Description = PaycheckDescription,
                    Amount = PaycheckAmount,
                    Date = recurring.NextDueDate,
                    CategoryId = 0,
                    Type = TransactionType.Income
                };
                TransactionService.AddTransaction(transaction);
            }
        }
        else
        {
            // One-time paycheck
            var transaction = new Transaction
            {
                Description = PaycheckDescription,
                Amount = PaycheckAmount,
                Date = PaycheckDate,
                CategoryId = 0,
                Type = TransactionType.Income
            };
            TransactionService.AddTransaction(transaction);
        }

        ShowPaycheckModal = false;
        LoadData();
    }

    private string GetFrequencyText()
    {
        return PaycheckFrequency switch
        {
            RecurrenceFrequency.Weekly => "week",
            RecurrenceFrequency.Biweekly => "2 weeks",
            RecurrenceFrequency.Monthly => "month",
            _ => "period"
        };
    }

    private string GetFrequencyLabel(RecurringTransaction r)
    {
        return r.Frequency switch
        {
            RecurrenceFrequency.Weekly => "Every week",
            RecurrenceFrequency.Biweekly => "Every 2 weeks",
            RecurrenceFrequency.Monthly => $"{DateHelpers.GetOrdinal(r.DayOfMonth)} of month",
            _ => "Recurring"
        };
    }

    private void DeletePaycheck(int transactionId)
    {
        TransactionService.DeleteTransaction(transactionId);
        LoadData();
    }

    private void DeleteRecurringPaycheck(int recurringId)
    {
        RecurringTransactionService.DeleteRecurringTransaction(recurringId);
        LoadData();
    }

    // Expected income editing
    private void StartEditingExpectedIncome()
    {
        if (!IsEditingExpectedIncome)
        {
            EditExpectedIncomeAmount = ExpectedIncome;
            IsEditingExpectedIncome = true;
        }
    }

    private void SaveExpectedIncome()
    {
        if (EditExpectedIncomeAmount >= 0)
        {
            SettingsService.SetMonthlyIncome(EditExpectedIncomeAmount);
            LoadData();
        }
        IsEditingExpectedIncome = false;
    }

    private void CancelEditExpectedIncome()
    {
        IsEditingExpectedIncome = false;
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

    private void OpenAddTransaction()
    {
        Navigation.NavigateTo("/add");
    }

    private void OpenCategory(int categoryId)
    {
        Navigation.NavigateTo($"/category/{categoryId}/{CurrentDate.Month}/{CurrentDate.Year}");
    }

    private void OpenFund(int fundId)
    {
        Navigation.NavigateTo($"/sinking-fund/{fundId}/detail");
    }
}
