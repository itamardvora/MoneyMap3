using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public class BudgetRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public BudgetRepository(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        public async Task Add(Budget budget)
        {
            await _db.InsertAsync(budget);
            App.BackupState?.MarkBudgetsChanged();
        }

        public async Task Update(Budget budget)
        {
            await _db.UpdateAsync(budget);
            App.BackupState?.MarkBudgetsChanged();
        }

        public async Task<Budget> GetUserBudget(int userId, int categoryId, DateTime month)
        {
            var allBudgets = await _db.Table<Budget>()
                .Where(b => b.UserID == userId && b.CategoryID == categoryId)
                .ToListAsync();

            var targetMonth = new DateTime(month.Year, month.Month, 1);
            return allBudgets.FirstOrDefault(b =>
                b.BudgetMonth.Year == targetMonth.Year &&
                b.BudgetMonth.Month == targetMonth.Month);
        }

        public async Task<List<Budget>> GetUserBudgets(int userId, DateTime? month = null)
        {
            var allBudgets = await _db.Table<Budget>()
                .Where(b => b.UserID == userId)
                .ToListAsync();

            if (month.HasValue)
            {
                var targetMonth = new DateTime(month.Value.Year, month.Value.Month, 1);
                allBudgets = allBudgets
                    .Where(b => b.BudgetMonth.Year == targetMonth.Year &&
                                b.BudgetMonth.Month == targetMonth.Month)
                    .ToList();
            }

            return allBudgets;
        }

        public async Task UpdateMonthlyLimit(int budgetId, decimal newMonthlyLimit)
        {
            var budget = await _db.Table<Budget>()
                                  .Where(b => b.BudgetID == budgetId)
                                  .FirstOrDefaultAsync();

            if (budget == null) return;

            budget.MonthlyLimit = newMonthlyLimit;
            await _db.UpdateAsync(budget);
            App.BackupState?.MarkBudgetsChanged();
        }
    }
}