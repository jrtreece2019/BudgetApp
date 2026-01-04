using SQLite;

namespace BudgetApp.Shared.Models;

public class UserSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1; // Single row table
    public decimal MonthlyIncome { get; set; } = 0;
}

