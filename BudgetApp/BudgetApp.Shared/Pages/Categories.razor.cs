using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class Categories : ComponentBase
{
    [Inject] private ICategoryService CategoryService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private List<Category> CategoryList { get; set; } = new();

    private List<Category> FixedCategories => CategoryList.Where(c => c.Type == CategoryType.Fixed).ToList();
    private List<Category> DiscretionaryCategories => CategoryList.Where(c => c.Type == CategoryType.Discretionary).ToList();
    private List<Category> SavingsCategories => CategoryList.Where(c => c.Type == CategoryType.Savings).ToList();

    private bool FixedExpanded { get; set; } = true;
    private bool DiscretionaryExpanded { get; set; } = true;
    private bool SavingsExpanded { get; set; } = true;

    private bool ShowModal { get; set; }
    private bool IsEditingCategory { get; set; }
    private int? EditCategoryId { get; set; }
    private string EditName { get; set; } = string.Empty;
    private string EditIcon { get; set; } = "📁";
    private string EditColor { get; set; } = "#6366f1";
    private decimal EditDefaultBudget { get; set; } = 0;
    private CategoryType EditType { get; set; } = CategoryType.Discretionary;
    private bool CanDelete { get; set; }

    private bool IsModalValid => !string.IsNullOrWhiteSpace(EditName);

    private readonly string[] IconOptions = new[]
    {
        "🍽️", "🚗", "📄", "🛍️", "🎬", "💊",
        "🏠", "💼", "🎮", "📚", "✈️", "🎵",
        "💪", "🐕", "👶", "🎁", "💰", "📱",
        "☕", "🍺", "🛒", "⚡", "💳", "🎓",
        "🛡️", "🎯", "📌", "📈", "🏦", "💵"
    };

    private readonly string[] ColorOptions = new[]
    {
        "#F59E0B", "#3B82F6", "#EF4444", "#EC4899", "#8B5CF6", "#10B981",
        "#F97316", "#06B6D4", "#84CC16", "#6366F1", "#14B8A6", "#F43F5E"
    };

    protected override void OnInitialized()
    {
        LoadData();
    }

    private void LoadData()
    {
        CategoryList = CategoryService.GetCategories();
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }

    private void AddCategory()
    {
        IsEditingCategory = false;
        EditCategoryId = null;
        EditName = string.Empty;
        EditIcon = "📁";
        EditColor = "#6366f1";
        EditDefaultBudget = 0;
        EditType = CategoryType.Discretionary;
        CanDelete = false;
        ShowModal = true;
    }

    private void EditCategory(Category category)
    {
        IsEditingCategory = true;
        EditCategoryId = category.Id;
        EditName = category.Name;
        EditIcon = category.Icon;
        EditColor = category.Color;
        EditDefaultBudget = category.DefaultBudget;
        EditType = category.Type;
        CanDelete = CategoryService.CanDeleteCategory(category.Id);
        ShowModal = true;
    }

    private void CloseModal()
    {
        ShowModal = false;
    }

    private void SaveCategory()
    {
        if (!IsModalValid) return;

        if (IsEditingCategory && EditCategoryId.HasValue)
        {
            var category = new Category
            {
                Id = EditCategoryId.Value,
                Name = EditName,
                Icon = EditIcon,
                Color = EditColor,
                DefaultBudget = EditDefaultBudget,
                Type = EditType
            };
            CategoryService.UpdateCategory(category);
        }
        else
        {
            var category = new Category
            {
                Name = EditName,
                Icon = EditIcon,
                Color = EditColor,
                DefaultBudget = EditDefaultBudget,
                Type = EditType
            };
            CategoryService.AddCategory(category);
        }

        CloseModal();
        LoadData();
    }

    // Confirm delete state
    private bool ShowDeleteConfirm { get; set; }

    private void ConfirmDeleteCategory()
    {
        ShowDeleteConfirm = true;
    }

    private void CancelDelete()
    {
        ShowDeleteConfirm = false;
    }

    private void DeleteCurrentCategory()
    {
        if (IsEditingCategory && EditCategoryId.HasValue && CanDelete)
        {
            CategoryService.DeleteCategory(EditCategoryId.Value);
            ShowDeleteConfirm = false;
            CloseModal();
            LoadData();
        }
    }
}
