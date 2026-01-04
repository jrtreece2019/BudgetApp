namespace BudgetApp.Shared.Services;

public enum AppTheme
{
    Dark,
    Light
}

public class ThemeService
{
    private AppTheme _currentTheme = AppTheme.Dark;
    
    public AppTheme CurrentTheme => _currentTheme;
    public bool IsDarkMode => _currentTheme == AppTheme.Dark;
    
    public event Action? OnThemeChanged;

    public void SetTheme(AppTheme theme)
    {
        if (_currentTheme != theme)
        {
            _currentTheme = theme;
            OnThemeChanged?.Invoke();
        }
    }

    public void ToggleTheme()
    {
        SetTheme(_currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }

    public string ThemeClass => _currentTheme == AppTheme.Dark ? "theme-dark" : "theme-light";
}

