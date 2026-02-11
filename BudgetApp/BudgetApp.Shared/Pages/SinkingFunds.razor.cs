using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class SinkingFunds : ComponentBase
{
    [Inject] private ISinkingFundService SinkingFundService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<SinkingFund> Funds { get; set; } = new();

    private IEnumerable<SinkingFund> ActiveFunds => Funds.Where(f => f.Status == SinkingFundStatus.Active);
    private IEnumerable<SinkingFund> PausedFunds => Funds.Where(f => f.Status == SinkingFundStatus.Paused);
    private IEnumerable<SinkingFund> CompletedFunds => Funds.Where(f => f.Status == SinkingFundStatus.Completed);

    private decimal TotalGoal => Funds.Sum(f => f.GoalAmount);
    private decimal TotalSaved => Funds.Sum(f => f.CurrentBalance);
    private double TotalPercentage => TotalGoal > 0 ? Math.Min(100, (double)(TotalSaved / TotalGoal) * 100) : 0;

    protected override void OnInitialized()
    {
        LoadFunds();
    }

    private void LoadFunds()
    {
        Funds = SinkingFundService.GetSinkingFunds();
    }

    private void AddNew()
    {
        Navigation.NavigateTo("/sinking-fund/new");
    }

    private void ViewFund(int id)
    {
        Navigation.NavigateTo($"/sinking-fund/{id}/detail");
    }
}
