namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Server-side Budget entity. Stores per-category, per-month budget overrides.
/// If a user hasn't customized a category's budget for a given month,
/// there's no Budget row -- the app falls back to Category.DefaultBudget.
/// </summary>
public class Budget
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
