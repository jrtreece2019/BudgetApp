namespace BudgetApp.Shared.Helpers;

/// <summary>
/// Shared date-formatting utilities used across multiple pages.
/// </summary>
public static class DateHelpers
{
    /// <summary>
    /// Returns a friendly label for a date: "Today", "Yesterday", or "MMM d".
    /// </summary>
    public static string FormatDate(DateTime date)
    {
        if (date.Date == DateTime.Today)
            return "Today";
        if (date.Date == DateTime.Today.AddDays(-1))
            return "Yesterday";
        return date.ToString("MMM d");
    }

    /// <summary>
    /// Returns an ordinal string like "1st", "2nd", "3rd", "4th", etc.
    /// </summary>
    public static string GetOrdinal(int number)
    {
        if (number >= 11 && number <= 13)
            return $"{number}th";

        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }
}
