using BudgetApp.Shared.Services;
using BudgetApp.Shared.Services.Interfaces;
using BudgetApp.Web.Components;
using BudgetApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the BudgetApp.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// SQLite database — register via the IDatabaseService interface
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BudgetApp", "budget.db3");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddSingleton<IDatabaseService>(new DatabaseService(dbPath));

// Split service registrations (one per domain concern)
builder.Services.AddSingleton<ICategoryService, SqliteCategoryService>();
builder.Services.AddSingleton<ITransactionService, SqliteTransactionService>();
builder.Services.AddSingleton<IBudgetService, SqliteBudgetService>();
builder.Services.AddSingleton<IRecurringTransactionService, SqliteRecurringTransactionService>();
builder.Services.AddSingleton<ISettingsService, SqliteSettingsService>();
builder.Services.AddSingleton<ISinkingFundService, SqliteSinkingFundService>();
builder.Services.AddSingleton<ThemeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(BudgetApp.Shared._Imports).Assembly);

app.Run();
