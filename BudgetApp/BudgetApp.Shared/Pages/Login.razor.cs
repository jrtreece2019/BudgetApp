using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

/// <summary>
/// Login page code-behind. Follows the same partial class pattern as Home.razor.cs.
///
/// The page injects IAuthService (which calls the backend API) and NavigationManager
/// (for redirecting after login). The form is simple: email + password + submit button.
///
/// If login succeeds, we navigate to Home. Sync is handled centrally by MainLayout
/// (which detects IsAuthenticated and kicks off SyncAsync + StartAutoSync).
/// </summary>
public partial class Login : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string Email { get; set; } = string.Empty;
    private string Password { get; set; } = string.Empty;
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    /// <summary>
    /// Basic client-side validation: both fields must have content.
    /// The server does the real validation (email format, password strength).
    /// </summary>
    private bool IsFormValid =>
        !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    /// <summary>
    /// If the user is already logged in, redirect them to the home page.
    /// No need to show the login form.
    /// </summary>
    protected override void OnInitialized()
    {
        if (AuthService.IsAuthenticated)
        {
            Navigation.NavigateTo("/");
        }
    }

    private async Task HandleLogin()
    {
        if (!IsFormValid || IsLoading) return;

        IsLoading = true;
        ErrorMessage = null;

        // Call the API via IAuthService. Returns null on success, error message on failure.
        var error = await AuthService.LoginAsync(Email, Password);

        if (error == null)
        {
            // Success -- navigate to Home.
            // Sync is handled by MainLayout.OnAfterRenderAsync which detects
            // IsAuthenticated and triggers SyncAsync + StartAutoSync.
            Navigation.NavigateTo("/");
        }
        else
        {
            ErrorMessage = error;
        }

        IsLoading = false;
    }
}
