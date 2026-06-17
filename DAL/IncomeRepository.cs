using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public class IncomeRepository
    {
        private readonly SQLiteAsyncConnection _db;
        private static bool _schemaEnsured = false;

        public IncomeRepository(SQLiteAsyncConnection db)
        {
            _db = db;
            _ = EnsureSchemaAsync();
        }

        // יוצר את טבלת ההכנסות והאינדקס הדרוש אם הם לא קיימים
        private async Task EnsureSchemaAsync()
        {
            if (_schemaEnsured)
                return;

            await _db.CreateTableAsync<Income>();
            await _db.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_incomes_user_date ON IncomesTable (UserID, Date)"); // יוצר אינדקס לשיפור מהירות חיפוש הכנסות לפי משתמש ותאריך

            _schemaEnsured = true;
        }

        public async Task AddIncome(Income income)
        {
            await _db.InsertAsync(income);
            App.BackupState?.MarkChanged();
        }

        // מחזיר את כל הכנסות המשתמש מהחדשה לישנה
        public Task<List<Income>> GetAllByUser(int userId)
        {
            return _db.Table<Income>()
                .Where(i => i.UserID == userId)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
        }

        // מחזיר את הכנסות המשתמש עבור חודש מסוים
        public Task<List<Income>> GetUserIncomes(int userId, DateTime month)
        {
            var start = new DateTime(month.Year, month.Month, 1);
            var end = start.AddMonths(1);

            return _db.Table<Income>()
                .Where(i => i.UserID == userId && i.Date >= start && i.Date < end)
                .ToListAsync();
        }


        // מחשב את סך כל ההכנסות של המשתמש בחודש מסוים
        public async Task<decimal> GetTotalIncomeByMonth(int userId, DateTime month)
        {
            var list = await GetUserIncomes(userId, month);
            decimal total = 0m;

            foreach (var income in list)
                total += income.Amount;

            return total;
        }

        public async Task DeleteIncome(int userId, int incomeId)
        {
            var income = await _db.Table<Income>()
                .Where(i => i.IncomeID == incomeId && i.UserID == userId)
                .FirstOrDefaultAsync();

            if (income != null)
            {
                await _db.DeleteAsync(income);
                App.BackupState?.MarkChanged();
            }
        }
    }
}