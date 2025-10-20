using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using MoneyMap.Models;
namespace MoneyMap.DAL
{
    public class InvestmentRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public InvestmentRepository(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        //  שליפת השקעה לפי מזהה השקעה + מזהה משתמש
        public async Task<Investment> GetById(int investmentId, int userId)
        {
            return await _db.Table<Investment>()
                            .Where(i => i.InvestmentID == investmentId && i.UserID == userId)
                            .FirstOrDefaultAsync();
        }

        //  שליפת כל ההשקעות של משתמש
        public async Task<List<Investment>> GetAllByUser(int userId)
        {
            return await _db.Table<Investment>()
                            .Where(i => i.UserID == userId)
                            .ToListAsync();
        }

        //  הוספת השקעה חדשה
        public async Task AddInvestment(Investment investment)
        {
            await _db.InsertAsync(investment);
        }

        //  מחיקת השקעה לפי מזהה השקעה + מזהה משתמש
        public async Task DeleteInvestment(int investmentId, int userId)
        {
            var investment = await GetById(investmentId, userId);
            if (investment != null)
            {
                await _db.DeleteAsync(investment);
            }
        }

        //  שליפת כל ההשקעות מסוג מניה מסוים (לפי סמל)
        public async Task<List<Investment>> GetBySymbol(int userId, string stockSymbol)
        {
            return await _db.Table<Investment>()
                            .Where(i => i.UserID == userId && i.StockSymbol == stockSymbol)
                            .ToListAsync();
        }
    }
}