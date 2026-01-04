using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services;

public interface IBudgetService
{
    List<Category> GetCategories();
    List<Transaction> GetTransactions(int month, int year);
    List<Budget> GetBudgets(int month, int year);
    
    decimal GetTotalBudget(int month, int year);
    decimal GetTotalSpent(int month, int year);
    decimal GetSpentByCategory(int categoryId, int month, int year);
    decimal GetBudgetByCategory(int categoryId, int month, int year);
    
    void AddTransaction(Transaction transaction);
    void UpdateTransaction(Transaction transaction);
    void DeleteTransaction(int transactionId);
    Transaction? GetTransaction(int transactionId);
    void UpdateBudget(int categoryId, int month, int year, decimal amount);
}

