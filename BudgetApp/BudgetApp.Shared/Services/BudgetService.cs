using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services;

public class BudgetService : IBudgetService
{
    private readonly List<Category> _categories;
    private readonly List<Transaction> _transactions;
    private readonly List<Budget> _budgets;
    private readonly List<RecurringTransaction> _recurringTransactions;
    private readonly List<SinkingFund> _sinkingFunds;
    private readonly List<SinkingFundTransaction> _sinkingFundTransactions;
    private int _nextTransactionId = 100;
    private int _nextCategoryId = 100;
    private int _nextRecurringId = 100;
    private int _nextSinkingFundId = 100;
    private int _nextSinkingFundTransactionId = 100;
    private decimal _monthlyIncome = 0;

    public BudgetService()
    {
        _categories = new List<Category>
        {
            new() { Id = 1, Name = "Food & Dining", Icon = "🍽️", Color = "#F59E0B", DefaultBudget = 500 },
            new() { Id = 2, Name = "Transport", Icon = "🚗", Color = "#3B82F6", DefaultBudget = 200 },
            new() { Id = 3, Name = "Bills & Utilities", Icon = "📄", Color = "#EF4444", DefaultBudget = 800 },
            new() { Id = 4, Name = "Shopping", Icon = "🛍️", Color = "#EC4899", DefaultBudget = 300 },
            new() { Id = 5, Name = "Entertainment", Icon = "🎬", Color = "#8B5CF6", DefaultBudget = 150 },
            new() { Id = 6, Name = "Health", Icon = "💊", Color = "#10B981", DefaultBudget = 100 }
        };

        var now = DateTime.Now;
        
        // No month-specific budgets needed - they'll use category defaults
        _budgets = new List<Budget>();

        _transactions = new List<Transaction>
        {
            new() { Id = 1, Description = "Grocery Store", Amount = 85.50m, Date = now.AddDays(-1), CategoryId = 1, Type = TransactionType.Expense },
            new() { Id = 2, Description = "Coffee Shop", Amount = 5.75m, Date = now, CategoryId = 1, Type = TransactionType.Expense },
            new() { Id = 3, Description = "Gas Station", Amount = 45.00m, Date = now.AddDays(-2), CategoryId = 2, Type = TransactionType.Expense },
            new() { Id = 4, Description = "Electric Bill", Amount = 120.00m, Date = now.AddDays(-5), CategoryId = 3, Type = TransactionType.Expense },
            new() { Id = 5, Description = "Internet", Amount = 79.99m, Date = now.AddDays(-5), CategoryId = 3, Type = TransactionType.Expense },
            new() { Id = 6, Description = "New Shoes", Amount = 89.99m, Date = now.AddDays(-3), CategoryId = 4, Type = TransactionType.Expense },
            new() { Id = 7, Description = "Netflix", Amount = 15.99m, Date = now.AddDays(-10), CategoryId = 5, Type = TransactionType.Expense },
            new() { Id = 8, Description = "Pharmacy", Amount = 25.00m, Date = now.AddDays(-4), CategoryId = 6, Type = TransactionType.Expense },
            new() { Id = 9, Description = "Paycheck", Amount = 2500.00m, Date = now.AddDays(-15), CategoryId = 1, Type = TransactionType.Income }
        };
        
        _recurringTransactions = new List<RecurringTransaction>();
        _sinkingFunds = new List<SinkingFund>();
        _sinkingFundTransactions = new List<SinkingFundTransaction>();
    }

    // Categories
    public List<Category> GetCategories() => _categories;

    public Category? GetCategory(int categoryId)
    {
        return _categories.FirstOrDefault(c => c.Id == categoryId);
    }

    public void AddCategory(Category category)
    {
        category.Id = _nextCategoryId++;
        _categories.Add(category);
    }

    public void UpdateCategory(Category category)
    {
        var existing = _categories.FirstOrDefault(c => c.Id == category.Id);
        if (existing != null)
        {
            existing.Name = category.Name;
            existing.Icon = category.Icon;
            existing.Color = category.Color;
            existing.DefaultBudget = category.DefaultBudget;
        }
    }

    public void DeleteCategory(int categoryId)
    {
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        if (category != null)
        {
            _categories.Remove(category);
        }
    }

    public bool CanDeleteCategory(int categoryId)
    {
        return !_transactions.Any(t => t.CategoryId == categoryId) &&
               !_recurringTransactions.Any(r => r.CategoryId == categoryId);
    }

    public List<Transaction> GetTransactions(int month, int year)
    {
        return _transactions
            .Where(t => t.Date.Month == month && t.Date.Year == year)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToList();
    }

    public List<Budget> GetBudgets(int month, int year)
    {
        return _budgets
            .Where(b => b.Month == month && b.Year == year)
            .ToList();
    }

    public decimal GetTotalBudget(int month, int year)
    {
        // Sum of custom budgets for this month + default budgets for categories without custom
        decimal total = 0;
        foreach (var category in _categories)
        {
            total += GetBudgetByCategory(category.Id, month, year);
        }
        return total;
    }

    public decimal GetTotalSpent(int month, int year)
    {
        return _transactions
            .Where(t => t.Date.Month == month && t.Date.Year == year && t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
    }

    public decimal GetSpentByCategory(int categoryId, int month, int year)
    {
        return _transactions
            .Where(t => t.CategoryId == categoryId && t.Date.Month == month && t.Date.Year == year && t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
    }

    public decimal GetBudgetByCategory(int categoryId, int month, int year)
    {
        // First check for month-specific custom budget
        var customBudget = _budgets.FirstOrDefault(b => b.CategoryId == categoryId && b.Month == month && b.Year == year);
        if (customBudget != null)
        {
            return customBudget.Amount;
        }
        
        // Fall back to category's default budget
        var category = _categories.FirstOrDefault(c => c.Id == categoryId);
        return category?.DefaultBudget ?? 0;
    }

    public bool IsBudgetCustom(int categoryId, int month, int year)
    {
        return _budgets.Any(b => b.CategoryId == categoryId && b.Month == month && b.Year == year);
    }

    public void ResetBudgetToDefault(int categoryId, int month, int year)
    {
        var budget = _budgets.FirstOrDefault(b => b.CategoryId == categoryId && b.Month == month && b.Year == year);
        if (budget != null)
        {
            _budgets.Remove(budget);
        }
    }

    public void AddTransaction(Transaction transaction)
    {
        transaction.Id = _nextTransactionId++;
        _transactions.Add(transaction);
    }

    public void UpdateTransaction(Transaction transaction)
    {
        var existing = _transactions.FirstOrDefault(t => t.Id == transaction.Id);
        if (existing != null)
        {
            existing.Description = transaction.Description;
            existing.Amount = transaction.Amount;
            existing.Date = transaction.Date;
            existing.CategoryId = transaction.CategoryId;
            existing.Type = transaction.Type;
        }
    }

    public void DeleteTransaction(int transactionId)
    {
        var transaction = _transactions.FirstOrDefault(t => t.Id == transactionId);
        if (transaction != null)
        {
            _transactions.Remove(transaction);
        }
    }

    public Transaction? GetTransaction(int transactionId)
    {
        return _transactions.FirstOrDefault(t => t.Id == transactionId);
    }

    public void UpdateBudget(int categoryId, int month, int year, decimal amount)
    {
        var budget = _budgets.FirstOrDefault(b => b.CategoryId == categoryId && b.Month == month && b.Year == year);
        if (budget != null)
        {
            budget.Amount = amount;
        }
        else
        {
            // Create new budget if it doesn't exist
            _budgets.Add(new Budget
            {
                Id = _budgets.Count + 1,
                CategoryId = categoryId,
                Month = month,
                Year = year,
                Amount = amount
            });
        }
    }

    // Recurring Transactions
    public List<RecurringTransaction> GetRecurringTransactions() => _recurringTransactions;

    public RecurringTransaction? GetRecurringTransaction(int id)
    {
        return _recurringTransactions.FirstOrDefault(r => r.Id == id);
    }

    public void AddRecurringTransaction(RecurringTransaction recurring)
    {
        recurring.Id = _nextRecurringId++;
        _recurringTransactions.Add(recurring);
    }

    public void UpdateRecurringTransaction(RecurringTransaction recurring)
    {
        var existing = _recurringTransactions.FirstOrDefault(r => r.Id == recurring.Id);
        if (existing != null)
        {
            existing.Description = recurring.Description;
            existing.Amount = recurring.Amount;
            existing.CategoryId = recurring.CategoryId;
            existing.Type = recurring.Type;
            existing.Frequency = recurring.Frequency;
            existing.DayOfMonth = recurring.DayOfMonth;
            existing.StartDate = recurring.StartDate;
            existing.NextDueDate = recurring.NextDueDate;
            existing.IsActive = recurring.IsActive;
        }
    }

    public void DeleteRecurringTransaction(int id)
    {
        var recurring = _recurringTransactions.FirstOrDefault(r => r.Id == id);
        if (recurring != null)
        {
            _recurringTransactions.Remove(recurring);
        }
    }

    public void ProcessRecurringTransactions()
    {
        var today = DateTime.Today;
        var dueRecurring = _recurringTransactions
            .Where(r => r.IsActive && r.NextDueDate <= today)
            .ToList();

        foreach (var recurring in dueRecurring)
        {
            while (recurring.NextDueDate <= today)
            {
                var transaction = new Transaction
                {
                    Id = _nextTransactionId++,
                    Description = recurring.Description,
                    Amount = recurring.Amount,
                    CategoryId = recurring.CategoryId,
                    Type = recurring.Type,
                    Date = recurring.NextDueDate
                };
                _transactions.Add(transaction);

                recurring.NextDueDate = CalculateNextDueDate(recurring);
            }
        }
    }

    private DateTime CalculateNextDueDate(RecurringTransaction recurring)
    {
        var current = recurring.NextDueDate;
        
        return recurring.Frequency switch
        {
            RecurrenceFrequency.Weekly => current.AddDays(7),
            RecurrenceFrequency.Biweekly => current.AddDays(14),
            RecurrenceFrequency.Monthly => GetNextMonthlyDate(current, recurring.DayOfMonth),
            RecurrenceFrequency.Yearly => current.AddYears(1),
            _ => current.AddMonths(1)
        };
    }

    private DateTime GetNextMonthlyDate(DateTime current, int dayOfMonth)
    {
        var nextMonth = current.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var day = Math.Min(dayOfMonth, daysInMonth);
        return new DateTime(nextMonth.Year, nextMonth.Month, day);
    }

    // Income & Settings
    public decimal GetMonthlyIncome() => _monthlyIncome;

    public void SetMonthlyIncome(decimal income)
    {
        _monthlyIncome = income;
    }

    public decimal GetTotalIncome(int month, int year)
    {
        return _transactions
            .Where(t => t.Date.Month == month && t.Date.Year == year && t.Type == TransactionType.Income)
            .Sum(t => t.Amount);
    }

    // Sinking Funds
    public List<SinkingFund> GetSinkingFunds() => _sinkingFunds;

    public SinkingFund? GetSinkingFund(int id)
    {
        return _sinkingFunds.FirstOrDefault(f => f.Id == id);
    }

    public void AddSinkingFund(SinkingFund fund)
    {
        fund.Id = _nextSinkingFundId++;
        _sinkingFunds.Add(fund);
    }

    public void UpdateSinkingFund(SinkingFund fund)
    {
        var existing = _sinkingFunds.FirstOrDefault(f => f.Id == fund.Id);
        if (existing != null)
        {
            existing.Name = fund.Name;
            existing.Icon = fund.Icon;
            existing.Color = fund.Color;
            existing.GoalAmount = fund.GoalAmount;
            existing.CurrentBalance = fund.CurrentBalance;
            existing.MonthlyContribution = fund.MonthlyContribution;
            existing.StartDate = fund.StartDate;
            existing.TargetDate = fund.TargetDate;
            existing.Status = fund.Status;
        }
    }

    public void DeleteSinkingFund(int id)
    {
        var fund = _sinkingFunds.FirstOrDefault(f => f.Id == id);
        if (fund != null)
        {
            // Remove associated transactions
            _sinkingFundTransactions.RemoveAll(t => t.SinkingFundId == id);
            _sinkingFunds.Remove(fund);
        }
    }

    public List<SinkingFundTransaction> GetSinkingFundTransactions(int fundId)
    {
        return _sinkingFundTransactions
            .Where(t => t.SinkingFundId == fundId)
            .OrderByDescending(t => t.Date)
            .ToList();
    }

    public void AddSinkingFundTransaction(SinkingFundTransaction transaction)
    {
        transaction.Id = _nextSinkingFundTransactionId++;
        _sinkingFundTransactions.Add(transaction);
        
        // Update fund balance
        var fund = _sinkingFunds.FirstOrDefault(f => f.Id == transaction.SinkingFundId);
        if (fund != null)
        {
            if (transaction.Type == SinkingFundTransactionType.Contribution)
            {
                fund.CurrentBalance += transaction.Amount;
            }
            else
            {
                fund.CurrentBalance -= transaction.Amount;
            }
            
            if (fund.CurrentBalance >= fund.GoalAmount)
            {
                fund.Status = SinkingFundStatus.Completed;
            }
        }
    }

    public void DeleteSinkingFundTransaction(int transactionId)
    {
        var transaction = _sinkingFundTransactions.FirstOrDefault(t => t.Id == transactionId);
        if (transaction != null)
        {
            // Reverse balance
            var fund = _sinkingFunds.FirstOrDefault(f => f.Id == transaction.SinkingFundId);
            if (fund != null)
            {
                if (transaction.Type == SinkingFundTransactionType.Contribution)
                {
                    fund.CurrentBalance -= transaction.Amount;
                }
                else
                {
                    fund.CurrentBalance += transaction.Amount;
                }
                
                if (fund.CurrentBalance < fund.GoalAmount && fund.Status == SinkingFundStatus.Completed)
                {
                    fund.Status = SinkingFundStatus.Active;
                }
            }
            
            _sinkingFundTransactions.Remove(transaction);
        }
    }

    public decimal GetTotalSinkingFundContributions(int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);
        
        return _sinkingFundTransactions
            .Where(t => t.Date >= startDate && t.Date < endDate 
                     && t.Type == SinkingFundTransactionType.Contribution)
            .Sum(t => t.Amount);
    }
}

