using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using MoneyMap.Models;

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
        private static bool _schemaEnsured = false;

        public ExpenseRepository(SQLiteAsyncConnection db)//  בודקת שקיימת טבלה אם לא קוראת לפעולה שתיצור 
        {
            _db = db;
            _ = EnsureSchemaAsync();
        }

        private async Task EnsureSchemaAsync()// מוודא שהדאטה בייס מוכן לשימוש 
        {
            if (_schemaEnsured) return;

            await _db.CreateTableAsync<Expense>();

            try
            {
                await _db.ExecuteAsync("ALTER TABLE ExpensesTable ADD COLUMN ReceiptPath TEXT");
            }
            catch
            {
                // exists – ignore
            }

            await _db.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_expenses_user_date ON ExpensesTable (UserID, Date)");

            _schemaEnsured = true;
        }

        public Task AddExpense(Expense expense) => _db.InsertAsync(expense);// הוספת הוצאה 

        public Task<List<Expense>> GetUserExpenses(int userId, DateTime month) // מחזיר רשימת הוצאות 
        {
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);

            return _db.Table<Expense>()
                      .Where(e => e.UserID == userId && e.Date >= start && e.Date < end)
                      .ToListAsync();
        }

        public Task<List<ExpenseCategoryTotal>> GetExpensesByCategory(int userId, DateTime month)// תחזיר כמה כסף בובז בכל קטגוריה 
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

        public async Task DeleteExpense(int userId, int expenseId)
        {
            var expense = await _db.Table<Expense>()
                                   .Where(e => e.ExpenseID == expenseId && e.UserID == userId)
                                   .FirstOrDefaultAsync();
            if (expense != null)
                await _db.DeleteAsync(expense);
        }

        public Task<Expense> GetById(int userId, int expenseId) =>
            _db.Table<Expense>()
               .Where(e => e.ExpenseID == expenseId && e.UserID == userId)
               .FirstOrDefaultAsync();
    }
}
