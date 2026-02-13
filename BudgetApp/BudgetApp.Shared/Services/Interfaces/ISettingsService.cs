namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Manages user settings like expected monthly income and income tracking.
/// </summary>
public interface ISettingsService
{
    decimal GetMonthlyIncome();
    void SetMonthlyIncome(decimal income);
    decimal GetTotalIncome(int month, int year);

    /// <summary>Returns true once the user has finished or skipped onboarding.</summary>
    bool HasCompletedOnboarding();

    /// <summary>Marks onboarding as complete so the user is never prompted again.</summary>
    void SetOnboardingComplete();
}
