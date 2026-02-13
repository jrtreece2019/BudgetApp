using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

/// <summary>
/// Code-behind for the Connect Bank page.
///
/// This page manages the Plaid Link flow:
/// 1. User taps "Connect Bank" → we get a link token from the API
/// 2. We open Plaid Link via JavaScript interop (Plaid's SDK is JS-only)
/// 3. Plaid Link calls back with a public token when the user finishes
/// 4. We send the public token to the API to complete the connection
///
/// The page also shows:
/// - Connected banks with their accounts and balances
/// - Unprocessed imported transactions waiting to be added to the budget
///
/// JS INTEROP: Plaid Link is a JavaScript library. Blazor can't run JS directly,
/// so we use IJSRuntime to call JavaScript functions defined in plaid-link.js.
/// The JS calls back into our C# code via a DotNetObjectReference.
/// </summary>
public partial class ConnectBank : ComponentBase, IDisposable
{
    [Inject] private IBankConnectionService BankService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private ISubscriptionService SubscriptionService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<ConnectedBank> ConnectedBanks { get; set; } = new();
    private List<ImportedTransactionView> UnprocessedTransactions { get; set; } = new();

    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; }
    private string? ErrorMessage { get; set; }
    private string? SuccessMessage { get; set; }

    /// <summary>
    /// Reference to this component that we pass to JavaScript.
    /// JS uses it to call our C# methods back (OnPlaidSuccess, OnPlaidExit).
    /// </summary>
    private DotNetObjectReference<ConnectBank>? _dotNetRef;

    protected override async Task OnInitializedAsync()
    {
        if (!AuthService.IsAuthenticated)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        _dotNetRef = DotNetObjectReference.Create(this);
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        ConnectedBanks = await BankService.GetConnectedBanksAsync();
        UnprocessedTransactions = await BankService.GetUnprocessedTransactionsAsync();

        IsLoading = false;
    }

    /// <summary>
    /// Starts the Plaid Link flow:
    /// 1. Request a link token from our API
    /// 2. Call the JavaScript function that opens Plaid Link with that token
    /// </summary>
    private async Task StartBankConnection()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        // Check premium status — bank connections are a paid feature.
        if (!SubscriptionService.IsPremium)
        {
            Navigation.NavigateTo("/upgrade");
            return;
        }

        var linkToken = await BankService.GetLinkTokenAsync();
        if (linkToken == null)
        {
            ErrorMessage = "Unable to start bank connection. Please try again.";
            return;
        }

        // Call the JavaScript function that opens Plaid Link.
        // We pass our DotNetObjectReference so JS can call us back.
        try
        {
            await JS.InvokeVoidAsync("PlaidLink.open", linkToken, _dotNetRef);
        }
        catch (JSException ex)
        {
            ErrorMessage = $"Failed to open bank connection: {ex.Message}";
        }
    }

    /// <summary>
    /// Called by JavaScript when the user successfully completes Plaid Link.
    /// The publicToken is a temporary token we exchange for a permanent access token.
    ///
    /// [JSInvokable] makes this method callable from JavaScript.
    /// </summary>
    [JSInvokable]
    public async Task OnPlaidSuccess(string publicToken)
    {
        IsProcessing = true;
        ErrorMessage = null;
        StateHasChanged(); // Tell Blazor to re-render (we're being called from JS).

        var result = await BankService.ExchangePublicTokenAsync(publicToken);

        if (result != null)
        {
            SuccessMessage = $"Successfully connected {result.InstitutionName}!";
            await LoadDataAsync();
        }
        else
        {
            ErrorMessage = "Failed to complete bank connection. Please try again.";
        }

        IsProcessing = false;
        StateHasChanged();
    }

    /// <summary>
    /// Called by JavaScript when the user exits Plaid Link without completing.
    /// This is normal (user cancelled) — we just clear any loading state.
    /// </summary>
    [JSInvokable]
    public Task OnPlaidExit(string? errorMessage)
    {
        if (!string.IsNullOrEmpty(errorMessage))
        {
            ErrorMessage = errorMessage;
        }

        IsProcessing = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes all unprocessed imported transactions: converts them into
    /// real budget transactions that show up in the app.
    /// </summary>
    private async Task ProcessAllTransactions()
    {
        IsProcessing = true;
        ErrorMessage = null;
        SuccessMessage = null;

        var count = await BankService.ProcessTransactionsAsync();

        if (count > 0)
        {
            SuccessMessage = $"Imported {count} transaction{(count == 1 ? "" : "s")} into your budget!";
        }

        await LoadDataAsync();
        IsProcessing = false;
    }

    /// <summary>
    /// Disconnects a bank after confirmation.
    /// </summary>
    private async Task DisconnectBank(ConnectedBank bank)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        IsProcessing = true;

        var success = await BankService.DisconnectBankAsync(bank.Id);

        if (success)
        {
            SuccessMessage = $"Disconnected {bank.InstitutionName}.";
            await LoadDataAsync();
        }
        else
        {
            ErrorMessage = "Failed to disconnect bank. Please try again.";
        }

        IsProcessing = false;
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }
}
