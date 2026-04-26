using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public class InvestmentRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public InvestmentRepository(SQLiteAsyncConnection db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<int> AddInvestment(Investment inv)
        {
            var result = await _db.InsertAsync(inv);
            App.BackupState?.MarkInvestmentsChanged();
            return result;
        }

        public async Task<int> DeleteInvestment(int investmentId, int userId)
        {
            var result = await _db.Table<Investment>()
               .Where(x => x.InvestmentID == investmentId && x.UserID == userId)
               .DeleteAsync();

            if (result > 0)
                App.BackupState?.MarkInvestmentsChanged();

            return result;
        }

        public Task<List<Investment>> GetAllByUser(int userId) =>
            _db.Table<Investment>()
               .Where(x => x.UserID == userId)
               .OrderByDescending(x => x.BuyDate)
               .ToListAsync();

        public Task<Investment> GetById(int id, int userId) =>
            _db.Table<Investment>()
               .Where(x => x.InvestmentID == id && x.UserID == userId)
               .FirstOrDefaultAsync();

        public Task<List<Investment>> GetBySymbol(int userId, string symbol) =>
            _db.Table<Investment>()
               .Where(x => x.UserID == userId && x.StockSymbol == symbol)
               .ToListAsync();
    }
}