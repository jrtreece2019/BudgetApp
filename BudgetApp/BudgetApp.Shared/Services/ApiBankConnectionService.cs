using System.Net.Http.Json;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// Implementation of IBankConnectionService that talks to the BudgetApp.Api
/// Plaid endpoints via HttpClient.
///
/// Every call first gets a valid JWT from IAuthService and attaches it as
/// a Bearer token. If the user isn't logged in, the calls return empty/null.
///
/// This follows the same pattern as ApiAuthService: HttpClient + JSON + error handling.
/// </summary>
public class ApiBankConnectionService : IBankConnectionService
{
    private readonly HttpClient _http;
    private readonly IAuthService _auth;

    public ApiBankConnectionService(HttpClient http, IAuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<string?> GetLinkTokenAsync()
    {
        if (!await SetAuthHeaderAsync()) return null;

        try
        {
            var response = await _http.PostAsync("api/plaid/link-token", null);
            if (!response.IsSuccessStatusCode) return null;

            var result = await response.Content.ReadFromJsonAsync<LinkTokenResponse>();
            return result?.LinkToken;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ExchangeTokenResponse?> ExchangePublicTokenAsync(string publicToken)
    {
        if (!await SetAuthHeaderAsync()) return null;

        try
        {
            var body = new { PublicToken = publicToken };
            var response = await _http.PostAsJsonAsync("api/plaid/exchange-token", body);

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<ExchangeTokenResponse>();
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<List<ConnectedBank>> GetConnectedBanksAsync()
    {
        if (!await SetAuthHeaderAsync()) return new();

        try
        {
            var result = await _http.GetFromJsonAsync<List<ConnectedBank>>("api/plaid/banks");
            return result ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    public async Task<bool> DisconnectBankAsync(int bankId)
    {
        if (!await SetAuthHeaderAsync()) return false;

        try
        {
            var response = await _http.DeleteAsync($"api/plaid/banks/{bankId}");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<List<ImportedTransactionView>> GetUnprocessedTransactionsAsync()
    {
        if (!await SetAuthHeaderAsync()) return new();

        try
        {
            var result = await _http.GetFromJsonAsync<List<ImportedTransactionView>>(
                "api/plaid/transactions");
            return result ?? new();
        }
        catch (HttpRequestException)
        {
            return new();
        }
    }

    public async Task<int> ProcessTransactionsAsync()
    {
        if (!await SetAuthHeaderAsync()) return 0;

        try
        {
            var response = await _http.PostAsync("api/plaid/process", null);
            if (!response.IsSuccessStatusCode) return 0;

            var result = await response.Content.ReadFromJsonAsync<ProcessResult>();
            return result?.Processed ?? 0;
        }
        catch (HttpRequestException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets a valid JWT token from IAuthService and sets it on the HttpClient.
    /// Returns false if the user isn't authenticated.
    /// </summary>
    private async Task<bool> SetAuthHeaderAsync()
    {
        var token = await _auth.GetAccessTokenAsync();
        if (token == null) return false;

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return true;
    }

    private class ProcessResult
    {
        public int Processed { get; set; }
    }
}
