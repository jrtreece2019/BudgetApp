using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class EditSinkingFund : ComponentBase
{
    [Inject] private ISinkingFundService SinkingFundService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int? FundId { get; set; }

    private SinkingFund Fund { get; set; } = new();
    private bool IsNew => FundId == null || FundId == 0;
    private bool IsValid => !string.IsNullOrWhiteSpace(Fund.Name) && Fund.GoalAmount > 0;

    private DateTime? TargetDateValue
    {
        get => Fund.TargetDate;
        set => Fund.TargetDate = value;
    }

    private int CalculatedMonths
    {
        get
        {
            if (Fund.MonthlyContribution <= 0) return 0;
            return (int)Math.Ceiling(Fund.GoalAmount / Fund.MonthlyContribution);
        }
    }

    private DateTime CalculatedDate => DateTime.Today.AddMonths(CalculatedMonths);

    private decimal RequiredMonthlyContribution
    {
        get
        {
            if (!TargetDateValue.HasValue || Fund.GoalAmount <= 0) return 0;
            var months = ((TargetDateValue.Value.Year - DateTime.Today.Year) * 12)
                       + TargetDateValue.Value.Month - DateTime.Today.Month;
            if (months <= 0) return Fund.GoalAmount;
            return Math.Ceiling(Fund.GoalAmount / months);
        }
    }

    private readonly string[] Icons = new[]
    {
        "🎯", "✈️", "🚗", "🏠", "🎁", "💍",
        "📱", "💻", "🎓", "🏥", "🛠️", "🎄",
        "🏖️", "🎉", "👶", "🐕", "📚", "🎸",
        "⚽", "🏋️", "🧳", "💄", "🪑", "🌴"
    };

    private readonly string[] Colors = new[]
    {
        "#6366F1", "#8B5CF6", "#EC4899", "#EF4444",
        "#F97316", "#F59E0B", "#10B981", "#14B8A6",
        "#06B6D4", "#3B82F6", "#6B7280", "#1F2937"
    };

    protected override void OnInitialized()
    {
        if (FundId.HasValue && FundId > 0)
        {
            var existing = SinkingFundService.GetSinkingFund(FundId.Value);
            if (existing != null)
            {
                Fund = existing;
            }
        }
        else
        {
            Fund.StartDate = DateTime.Today;
        }
    }

    private void ApplySuggestedContribution()
    {
        Fund.MonthlyContribution = RequiredMonthlyContribution;
    }

    private void Save()
    {
        if (!IsValid) return;

        if (IsNew)
        {
            SinkingFundService.AddSinkingFund(Fund);
        }
        else
        {
            SinkingFundService.UpdateSinkingFund(Fund);
        }

        Navigation.NavigateTo("/sinking-funds");
    }

    // Confirm delete state
    private bool ShowDeleteConfirm { get; set; }

    private void ConfirmDelete()
    {
        ShowDeleteConfirm = true;
    }

    private void CancelDelete()
    {
        ShowDeleteConfirm = false;
    }

    private void Delete()
    {
        if (FundId.HasValue)
        {
            SinkingFundService.DeleteSinkingFund(FundId.Value);
        }
        ShowDeleteConfirm = false;
        Navigation.NavigateTo("/sinking-funds");
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/sinking-funds");
    }
}
