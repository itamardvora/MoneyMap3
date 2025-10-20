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
    public class ExpenseCategoryTotal
    {
        public int CategoryID { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ExpenseRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public ExpenseRepository(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        // Create: מוסיף הוצאה חדשה (אובייקט מלא)
        public Task AddExpense(Expense expense)
        {
            // מומלץ לוודא בשכבת Service: מילוי CreatedAt, ולידציות, וכו'
            return _db.InsertAsync(expense);
        }

        // Read: כל ההוצאות של המשתמש בחודש מסוים
        public Task<List<Expense>> GetUserExpenses(int userId, DateTime month)
        {
            // סינון לפי משתמש + חודש/שנה
            return _db.Table<Expense>()
                      .Where(e => e.UserID == userId
                               && e.Date.Year == month.Year
                               && e.Date.Month == month.Month)
                      .ToListAsync();
        }

        // Read (אגרגציה): סכום הוצאות לפי קטגוריה בחודש (יעיל עם SQL ישיר)
        public Task<List<ExpenseCategoryTotal>> GetExpensesByCategory(int userId, DateTime month)
        {
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);

            const string sql = @"
        SELECT CategoryID AS CategoryID,
               SUM(Amount) AS TotalAmount
        FROM ExpensesTable
        WHERE UserID = ?
          AND Date >= ? AND Date < ?
        GROUP BY CategoryID";

            return _db.QueryAsync<ExpenseCategoryTotal>(sql, userId, start, end);
        }


        // Delete: מוחק הוצאה לפי מזהה, רק אם שייכת למשתמש
        public async Task DeleteExpense(int userId, int expenseId)
        {
            var expense = await _db.Table<Expense>()
                                   .Where(e => e.ExpenseID == expenseId && e.UserID == userId)
                                   .FirstOrDefaultAsync();
            if (expense != null)
            {
                await _db.DeleteAsync(expense);
            }
        }

        // אופציונלי: שליפה לפי מזהה (לפעמים שימושי לשירותים)
        public Task<Expense> GetById(int userId, int expenseId)
        {
            return _db.Table<Expense>()
                      .Where(e => e.ExpenseID == expenseId && e.UserID == userId)
                      .FirstOrDefaultAsync();
        }
    }
}