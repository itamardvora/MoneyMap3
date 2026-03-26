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

        // הוספה
        public Task<int> AddInvestment(Investment inv) => _db.InsertAsync(inv);


        // מחיקה מוגנת לפי משתמש
        public Task<int> DeleteInvestment(int investmentId, int userId) =>
            _db.Table<Investment>()
               .Where(x => x.InvestmentID == investmentId && x.UserID == userId)
               .DeleteAsync();


        // שליפת מניות של משתמש 
        public Task<List<Investment>> GetAllByUser(int userId) =>
            _db.Table<Investment>()
               .Where(x => x.UserID == userId)
               .OrderByDescending(x => x.BuyDate)
               .ToListAsync();



        //public Task<List<Investment>> GetByUserId(int userId) => GetAllByUser(userId);



        // מחזיר השקעה אחת ספציפית לשם המנייה המבוקשת 
        public Task<Investment> GetById(int id, int userId) =>
            _db.Table<Investment>()
               .Where(x => x.InvestmentID == id && x.UserID == userId)
               .FirstOrDefaultAsync();

        // מחזיר את כל ההשקעות לפי מנייה מסוימת 
        public Task<List<Investment>> GetBySymbol(int userId, string symbol) =>
            _db.Table<Investment>()
               .Where(x => x.UserID == userId && x.StockSymbol == symbol)
               .ToListAsync();
    }
}
