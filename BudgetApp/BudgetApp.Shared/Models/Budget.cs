using SQLite;

namespace BudgetApp.Shared.Models;

public class Budget
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

