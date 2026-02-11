using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Manages budget categories (Fixed, Discretionary, Savings).
/// </summary>
public interface ICategoryService
{
    List<Category> GetCategories();
    Category? GetCategory(int categoryId);
    void AddCategory(Category category);
    void UpdateCategory(Category category);
    void DeleteCategory(int categoryId);
    bool CanDeleteCategory(int categoryId);
}
