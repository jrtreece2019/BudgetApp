namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Server-side SinkingFund entity. A sinking fund is a savings goal where
/// you contribute a fixed amount monthly toward a target (e.g., "Vacation $3000").
/// Computed properties (RemainingAmount, ProgressPercentage, etc.) are NOT stored
/// server-side -- they're calculated on the client from the raw data.
/// </summary>
public class SinkingFund
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal GoalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal MonthlyContribution { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public SinkingFundStatus Status { get; set; }
    public bool AutoContribute { get; set; }
    public DateTime? LastAutoContributeDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // Navigation property
    public AppUser User { get; set; } = null!;
}
