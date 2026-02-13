namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Server-side UserSettings entity. Stores per-user app settings.
/// On the client side, this always has Id=1 (single user). On the server,
/// each user gets their own row identified by UserId.
/// </summary>
public class UserSettings
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public decimal MonthlyIncome { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // Navigation property
    public AppUser User { get; set; } = null!;
}
