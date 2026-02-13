namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Server-side SinkingFundTransaction entity. Tracks individual contributions
/// to and withdrawals from a SinkingFund. SinkingFundId references the
/// server-side SinkingFund (mapped from client SyncId during sync).
/// </summary>
public class SinkingFundTransaction
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public int SinkingFundId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public SinkingFundTransactionType Type { get; set; }
    public string Note { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // Navigation properties
    public AppUser User { get; set; } = null!;
    public SinkingFund SinkingFund { get; set; } = null!;
}
