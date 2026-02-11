using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// SQLite-backed implementation of ICategoryService.
/// Delegates data access to IDatabaseService and adds business rules.
/// </summary>
public class SqliteCategoryService : ICategoryService
{
    private readonly IDatabaseService _db;

    public SqliteCategoryService(IDatabaseService db)
    {
        _db = db;
    }

    public List<Category> GetCategories()
        => _db.GetCategories();

    public Category? GetCategory(int categoryId)
        => _db.GetCategory(categoryId);

    public void AddCategory(Category category)
        => _db.AddCategory(category);

    public void UpdateCategory(Category category)
        => _db.UpdateCategory(category);

    public void DeleteCategory(int categoryId)
        => _db.DeleteCategory(categoryId);

    /// <summary>
    /// A category can only be deleted if no transactions or recurring
    /// transactions reference it — prevents orphaned data.
    /// </summary>
    public bool CanDeleteCategory(int categoryId)
        => !_db.HasTransactionsForCategory(categoryId)
        && !_db.HasRecurringTransactionsForCategory(categoryId);
}
