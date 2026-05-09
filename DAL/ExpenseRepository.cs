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

        public ExpenseRepository(SQLiteAsyncConnection db)
        {
            _db = db;
            _ = EnsureSchemaAsync();
        }

        private async Task EnsureSchemaAsync()
        {
            if (_schemaEnsured)
                return;

            await _db.CreateTableAsync<Expense>();

            try
            {
                await _db.ExecuteAsync("ALTER TABLE ExpensesTable ADD COLUMN ReceiptPath TEXT");
            }
            catch
            {
            }

            await _db.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_expenses_user_date ON ExpensesTable (UserID, Date)");

            _schemaEnsured = true;
        }

        public async Task AddExpense(Expense expense)
        {
            await _db.InsertAsync(expense);
            App.BackupState?.MarkChanged();
        }

        public Task<List<Expense>> GetAllByUser(int userId)
        {
            return _db.Table<Expense>()
                .Where(e => e.UserID == userId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public Task<List<Expense>> GetUserExpenses(int userId, DateTime month)
        {
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);

            return _db.Table<Expense>()
                .Where(e => e.UserID == userId && e.Date >= start && e.Date < end)
                .ToListAsync();
        }

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

        public async Task DeleteExpense(int userId, int expenseId)
        {
            var expense = await _db.Table<Expense>()
                .Where(e => e.ExpenseID == expenseId && e.UserID == userId)
                .FirstOrDefaultAsync();

            if (expense != null)
            {
                await _db.DeleteAsync(expense);
                App.BackupState?.MarkChanged();
            }
        }

        public Task<Expense> GetById(int userId, int expenseId)
        {
            return _db.Table<Expense>()
                .Where(e => e.ExpenseID == expenseId && e.UserID == userId)
                .FirstOrDefaultAsync();
        }
    }
}