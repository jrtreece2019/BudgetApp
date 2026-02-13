using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

/// <summary>
/// Register page code-behind. Creates a new user account via the backend API.
///
/// Has one extra field vs Login: ConfirmPassword. The client checks that both
/// passwords match before sending. The server validates password strength
/// (8+ chars, upper/lower/digit) via ASP.NET Identity rules.
///
/// After successful registration, we navigate to Home. Sync is handled
/// centrally by MainLayout (which detects IsAuthenticated).
/// </summary>
public partial class Register : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string Email { get; set; } = string.Empty;
    private string Password { get; set; } = string.Empty;
    private string ConfirmPassword { get; set; } = string.Empty;
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }

    private bool IsFormValid =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Password) &&
        Password == ConfirmPassword &&
        Password.Length >= 8;

    protected override void OnInitialized()
    {
        if (AuthService.IsAuthenticated)
        {
            Navigation.NavigateTo("/");
        }
    }

    private async Task HandleRegister()
    {
        if (!IsFormValid || IsLoading) return;

        IsLoading = true;
        ErrorMessage = null;

        var error = await AuthService.RegisterAsync(Email, Password);

        if (error == null)
        {
            // Success -- user is now logged in, go to home.
            // Sync is handled by MainLayout.OnAfterRenderAsync.
            Navigation.NavigateTo("/");
        }
        else
        {
            ErrorMessage = error;
        }

        IsLoading = false;
    }
}
