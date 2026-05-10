using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoneyMap.DAL;
using MoneyMap.Models;

namespace MoneyMap.Services
{
    public class BudgetService
    {
        private readonly BudgetRepository _budgetRepo;
        private readonly ExpenseRepository _expenseRepo;
        private readonly CategoryRepository _categoryRepo;

        public BudgetService(BudgetRepository budgetRepo, ExpenseRepository expenseRepo, CategoryRepository categoryRepo)
        {
            _budgetRepo = budgetRepo;
            _expenseRepo = expenseRepo;
            _categoryRepo = categoryRepo;
        }

        public async Task AddUserBudget(int userId, int categoryId, decimal monthlyLimit, DateTime budgetMonth)
        {
            if (monthlyLimit <= 0)
                throw new ArgumentException("Monthly limit must be positive");

            var month = new DateTime(budgetMonth.Year, budgetMonth.Month, 1);

            // אין כפילות
            var existing = await _budgetRepo.GetUserBudget(userId, categoryId, month);
            if (existing != null)
                throw new InvalidOperationException("Budget for this category and month already exists");

            // וידוא שהקטגוריה קיימת למשתמש
            var userCats = await _categoryRepo.GetCategoriesForUser(userId);
            if (!userCats.Any(c => c.CategoryID == categoryId))
                throw new ArgumentException("Category does not exist for this user");

            var budget = new Budget
            {
                UserID = userId,
                CategoryID = categoryId,
                MonthlyLimit = monthlyLimit,
                BudgetMonth = month,
                CreatedAt = DateTime.Now
            };

            await _budgetRepo.Add(budget);
        }

        public async Task<List<BudgetStatus>> GetUserBudgetStatus(int userId, DateTime budgetMonth)
        {
            var month = new DateTime(budgetMonth.Year, budgetMonth.Month, 1);

            var budgets = await _budgetRepo.GetUserBudgets(userId, month);
            var totalsList = await _expenseRepo.GetExpensesByCategory(userId, month);
            var totalsByCategory = totalsList.ToDictionary(x => x.CategoryID, x => x.TotalAmount);
            var categories = await _categoryRepo.GetCategoriesForUser(userId);

            var result = new List<BudgetStatus>();
            foreach (var b in budgets)
            {
                var spent = totalsByCategory.TryGetValue(b.CategoryID, out var val) ? val : 0m;
                var remaining = b.MonthlyLimit - spent;
                var usagePct = b.MonthlyLimit > 0 ? Math.Round((double)(spent / b.MonthlyLimit) * 100, 2) : 0.0;
                var categoryName = categories.FirstOrDefault(c => c.CategoryID == b.CategoryID)?.CategoryName ?? "קטגוריה לא ידועה";

                result.Add(new BudgetStatus
                {
                    BudgetID = b.BudgetID,
                    CategoryID = b.CategoryID,
                    MonthlyLimit = b.MonthlyLimit,
                    Spent = spent,
                    Remaining = remaining,
                    UsagePct = usagePct,
                    CategoryName = categoryName
                });
            }

            return result;
        }

        public async Task<Dictionary<int, decimal>> GetRemainingBudget(int userId, DateTime budgetMonth)
        {
            var month = new DateTime(budgetMonth.Year, budgetMonth.Month, 1);

            var budgets = await _budgetRepo.GetUserBudgets(userId, month);
            var totalsList = await _expenseRepo.GetExpensesByCategory(userId, month);
            var totalsByCategory = totalsList.ToDictionary(x => x.CategoryID, x => x.TotalAmount);

            var remaining = new Dictionary<int, decimal>();
            foreach (var b in budgets)
            {
                var spent = totalsByCategory.TryGetValue(b.CategoryID, out var val) ? val : 0m;
                remaining[b.CategoryID] = b.MonthlyLimit - spent;
            }

            return remaining;
        }

        
    }
}
