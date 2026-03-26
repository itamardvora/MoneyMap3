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
            await _db.InsertAsync(budget);
            // קבלת פרטים והכנסה לטבלת נתונים 
        }

        public async Task Update(Budget budget)
        {
            await _db.UpdateAsync(budget);
        }

        public async Task<Budget> GetUserBudget(int userId, int categoryId, DateTime month)// מחזיר תקציב אחד 
        {
            //  תביא את כל התקציבים של המשתמש והקטגוריה מהמסד עדיין לא ממין לפי חודש 
            var allBudgets = await _db.Table<Budget>()
                .Where(b => b.UserID == userId && b.CategoryID == categoryId)
                .ToListAsync();

            // תסנן לפי חודש ושנה בזיכרון 
            var targetMonth = new DateTime(month.Year, month.Month, 1);// ה1 הוא סוג של סתם יום גנארי בשביל שהוא ישלוף הכל מאותו החודש 
            return allBudgets.FirstOrDefault(b =>
                b.BudgetMonth.Year == targetMonth.Year &&
                b.BudgetMonth.Month == targetMonth.Month);
        }


      
        public async Task<List<Budget>> GetUserBudgets(int userId, DateTime? month = null)// מחזיר רשימת תקציבים
        {
            //  שלוף את כל התקציבים של המשתמש
            var allBudgets = await _db.Table<Budget>()
                .Where(b => b.UserID == userId)
                .ToListAsync();

            // : סנן לפי חודש אם נדרש
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