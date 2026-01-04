using BudgetApp.Services;
using BudgetApp.Shared.Services;
using Microsoft.Extensions.Logging;

namespace BudgetApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the BudgetApp.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            
            // SQLite database path for mobile/desktop
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "budget.db3");
            builder.Services.AddSingleton(new DatabaseService(dbPath));
            builder.Services.AddSingleton<IBudgetService, SqliteBudgetService>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
