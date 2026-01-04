using SQLite;

namespace BudgetApp.Shared.Models;

public enum SinkingFundStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2
}

public class SinkingFund
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎯";
    public string Color { get; set; } = "#6366F1";
    
    public decimal GoalAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal MonthlyContribution { get; set; }
    
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? TargetDate { get; set; }
    
    public SinkingFundStatus Status { get; set; } = SinkingFundStatus.Active;
    
    public bool AutoContribute { get; set; } = false;
    
    public DateTime? LastAutoContributeDate { get; set; }
    
    // Computed properties (not stored in DB)
    [Ignore]
    public decimal RemainingAmount => GoalAmount - CurrentBalance;
    
    [Ignore]
    public double ProgressPercentage => GoalAmount > 0 
        ? Math.Min(100, (double)(CurrentBalance / GoalAmount) * 100) 
        : 0;
    
    [Ignore]
    public int MonthsRemaining
    {
        get
        {
            if (TargetDate.HasValue)
            {
                var months = ((TargetDate.Value.Year - DateTime.Today.Year) * 12) 
                           + TargetDate.Value.Month - DateTime.Today.Month;
                return Math.Max(0, months);
            }
            if (MonthlyContribution > 0 && RemainingAmount > 0)
            {
                return (int)Math.Ceiling(RemainingAmount / MonthlyContribution);
            }
            return 0;
        }
    }
    
    [Ignore]
    public DateTime? ProjectedCompletionDate
    {
        get
        {
            if (MonthlyContribution > 0 && RemainingAmount > 0)
            {
                var monthsNeeded = (int)Math.Ceiling(RemainingAmount / MonthlyContribution);
                return DateTime.Today.AddMonths(monthsNeeded);
            }
            if (CurrentBalance >= GoalAmount)
            {
                return DateTime.Today;
            }
            return null;
        }
    }
    
    [Ignore]
    public bool IsOnTrack
    {
        get
        {
            if (!TargetDate.HasValue || MonthlyContribution <= 0) return true;
            
            // Calculate expected balance based on months since start
            var monthsSinceStart = ((DateTime.Today.Year - StartDate.Year) * 12) 
                                 + DateTime.Today.Month - StartDate.Month;
            var expectedBalance = monthsSinceStart * MonthlyContribution;
            
            return CurrentBalance >= expectedBalance * 0.9m; // Within 10% is "on track"
        }
    }
}

