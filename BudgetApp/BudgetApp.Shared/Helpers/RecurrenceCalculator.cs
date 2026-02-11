using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Helpers;

/// <summary>
/// Centralised helper for calculating recurring-transaction due dates.
/// Exists in one place so the logic is never duplicated across services.
/// </summary>
public static class RecurrenceCalculator
{
    /// <summary>
    /// Given a recurring transaction, returns the next due date after its current NextDueDate.
    /// </summary>
    public static DateTime CalculateNextDueDate(RecurringTransaction recurring)
    {
        var current = recurring.NextDueDate;

        return recurring.Frequency switch
        {
            RecurrenceFrequency.Weekly   => current.AddDays(7),
            RecurrenceFrequency.Biweekly => current.AddDays(14),
            RecurrenceFrequency.Monthly  => GetNextMonthlyDate(current, recurring.DayOfMonth),
            RecurrenceFrequency.Yearly   => current.AddYears(1),
            _                            => current.AddMonths(1)
        };
    }

    /// <summary>
    /// Calculates the same day-of-month in the next calendar month,
    /// clamping to the last day when the target month is shorter.
    /// </summary>
    public static DateTime GetNextMonthlyDate(DateTime current, int dayOfMonth)
    {
        var nextMonth = current.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var day = Math.Min(dayOfMonth, daysInMonth);
        return new DateTime(nextMonth.Year, nextMonth.Month, day);
    }
}
