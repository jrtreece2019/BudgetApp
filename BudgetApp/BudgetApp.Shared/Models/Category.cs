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

    /// <summary>
    /// Globally unique ID for matching this record across devices during sync.
    /// Generated once at creation, never changes. Different from Id which is
    /// auto-incremented locally and differs on each device.
    /// </summary>
    public Guid SyncId { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = "#6B7280";
    public decimal DefaultBudget { get; set; } = 0;
    public CategoryType Type { get; set; } = CategoryType.Discretionary;

    /// <summary>
    /// UTC timestamp of the last insert or update. Used by the sync service
    /// to find records that changed since the last sync.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft-delete flag. When true, the record is treated as deleted locally
    /// but kept in the database so the sync service can propagate the deletion
    /// to other devices and the server.
    /// </summary>
    public bool IsDeleted { get; set; }
}

