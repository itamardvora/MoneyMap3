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
    public class StockPriceCacheRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public StockPriceCacheRepository(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        // מחזיר את הרשומה של מניה לפי הסמל
        public async Task<StockPrices> GetBySymbol(string stockSymbol)
        {
            return await _db.Table<StockPrices>()
                            .Where(s => s.StockSymbol == stockSymbol)
                            .FirstOrDefaultAsync();
        }

        // מעדכן מחיר ותאריך עדכון למניה מסוימת
        public async Task Update(string stockSymbol, decimal price, DateTime updatedAt)
        {
            var existing = await GetBySymbol(stockSymbol);
            if (existing != null)
            {
                existing.LastPrice = price;
                existing.LastUpdated = updatedAt;
                await _db.UpdateAsync(existing);
            }
        }

        // מוסיף או מעדכן רשומת מניה
        public async Task InsertOrUpdate(string stockSymbol, decimal price)
        {
            var existing = await GetBySymbol(stockSymbol);
            if (existing != null)
            {
                existing.LastPrice = price;
                existing.LastUpdated = DateTime.Now;
                await _db.UpdateAsync(existing);
            }
            else
            {
                var newEntry = new StockPrices
                {
                    StockSymbol = stockSymbol,
                    LastPrice = price,
                    LastUpdated = DateTime.Now
                };
                await _db.InsertAsync(newEntry);
            }
        }

        // בודק אם המטמון עדיין תקף (פחות משעה)
        public async Task<bool> IsCacheValid(string stockSymbol)
        {
            var existing = await GetBySymbol(stockSymbol);
            if (existing == null) return false;

            var timeSinceUpdate = DateTime.Now - existing.LastUpdated;
            return timeSinceUpdate.TotalHours < 24; 
        }

    }
}