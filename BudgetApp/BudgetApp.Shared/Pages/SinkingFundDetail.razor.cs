using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;
using BudgetApp.Shared.Helpers;

namespace BudgetApp.Shared.Pages;

public partial class SinkingFundDetail : ComponentBase
{
    [Inject] private ISinkingFundService SinkingFundService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public int FundId { get; set; }

    private SinkingFund? Fund { get; set; }
    private List<SinkingFundTransaction> Transactions { get; set; } = new();

    private bool ShowTransactionModal { get; set; }
    private SinkingFundTransactionType NewTransactionType { get; set; }
    private decimal NewTransactionAmount { get; set; }
    private DateTime NewTransactionDate { get; set; } = DateTime.Today;
    private string NewTransactionNote { get; set; } = string.Empty;

    protected override void OnInitialized()
    {
        LoadData();
    }

    private void LoadData()
    {
        Fund = SinkingFundService.GetSinkingFund(FundId);
        if (Fund != null)
        {
            Transactions = SinkingFundService.GetSinkingFundTransactions(FundId);
        }
    }

    private string GetStatusClass()
    {
        return Fund?.Status switch
        {
            SinkingFundStatus.Active => "active",
            SinkingFundStatus.Paused => "paused",
            SinkingFundStatus.Completed => "completed",
            _ => ""
        };
    }

    private void ShowAddTransaction(SinkingFundTransactionType type)
    {
        NewTransactionType = type;
        NewTransactionAmount = 0;
        NewTransactionDate = DateTime.Today;
        NewTransactionNote = string.Empty;
        ShowTransactionModal = true;
    }

    private void CloseModal()
    {
        ShowTransactionModal = false;
    }

    private void SaveTransaction()
    {
        if (NewTransactionAmount <= 0) return;

        var transaction = new SinkingFundTransaction
        {
            SinkingFundId = FundId,
            Amount = NewTransactionAmount,
            Type = NewTransactionType,
            Date = NewTransactionDate,
            Note = NewTransactionNote
        };

        SinkingFundService.AddSinkingFundTransaction(transaction);
        LoadData();
        CloseModal();
    }

    // Confirm delete state
    private bool ShowDeleteConfirm { get; set; }
    private int? PendingDeleteTransactionId { get; set; }

    private void ConfirmDeleteTransaction(int transactionId)
    {
        PendingDeleteTransactionId = transactionId;
        ShowDeleteConfirm = true;
    }

    private void CancelDelete()
    {
        ShowDeleteConfirm = false;
        PendingDeleteTransactionId = null;
    }

    private void DeleteTransaction()
    {
        if (PendingDeleteTransactionId.HasValue)
        {
            SinkingFundService.DeleteSinkingFundTransaction(PendingDeleteTransactionId.Value);
            LoadData();
        }
        ShowDeleteConfirm = false;
        PendingDeleteTransactionId = null;
    }

    private void EditFund()
    {
        Navigation.NavigateTo($"/sinking-fund/{FundId}");
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/sinking-funds");
    }
}
