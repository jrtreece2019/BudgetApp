using SQLite;

namespace BudgetApp.Shared.Models;

public enum CategoryType
{
    Fixed = 0,
    Discretionary = 1,
    Savings = 2
}

public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = "#6B7280";
    public decimal DefaultBudget { get; set; } = 0;
    public CategoryType Type { get; set; } = CategoryType.Discretionary;
}

