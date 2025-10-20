using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using System;
using System.IO;
using Xamarin.Essentials;
using MoneyMap.Models;
using System.Threading.Tasks; 


namespace MoneyMap.DAL
{
    public class BudgetRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public BudgetRepository(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        // Create: מקבל אובייקט מלא
        public async Task Add(Budget budget)
        {
            // אחריות על מילוי CreatedAt/ניקוי שדות מסוכנים תהיה ב-Service
            await _db.InsertAsync(budget);
        }

        // Update: מקבל אובייקט מלא
        public async Task Update(Budget budget)
        {
            await _db.UpdateAsync(budget);
        }

        // Read: שליפה של תקציב ספציפי (לפי משתמש+קטגוריה+חודש)
        public async Task<Budget> GetUserBudget(int userId, int categoryId, DateTime month)
        {
            // תביא את כל התקציבים של המשתמש והקטגוריה מהמסד
            var allBudgets = await _db.Table<Budget>()
                .Where(b => b.UserID == userId && b.CategoryID == categoryId)
                .ToListAsync();

            // תסנן לפי חודש ושנה בזיכרון (C#)
            var targetMonth = new DateTime(month.Year, month.Month, 1);
            return allBudgets.FirstOrDefault(b =>
                b.BudgetMonth.Year == targetMonth.Year &&
                b.BudgetMonth.Month == targetMonth.Month);
        }


        // Read: כל התקציבים של משתמש (אופציונלי לפי חודש)
        public async Task<List<Budget>> GetUserBudgets(int userId, DateTime? month = null)
        {
            // שלב 1: שלוף את כל התקציבים של המשתמש
            var allBudgets = await _db.Table<Budget>()
                .Where(b => b.UserID == userId)
                .ToListAsync();

            // שלב 2: סנן לפי חודש (אם נדרש)
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
        }

    }
}