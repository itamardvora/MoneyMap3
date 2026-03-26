using SQLite;
using System;
using System.Threading.Tasks;
using System.Linq;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public class CurrencyRateRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public CurrencyRateRepository(SQLiteAsyncConnection db)
        {
            _db = db;
            _db.CreateTableAsync<CurrencyRate>().Wait();// יצירת טבלה חדשה אם לא קיימת 
        }

        // מביא שער לפי קוד מטבע ("USD", "EUR", "ILS")
        public async Task<CurrencyRate> GetRateAsync(string code)
        {
            return await _db.Table<CurrencyRate>()
                            .Where(r => r.Code == code)
                            .FirstOrDefaultAsync();
        }



// הוספה או עדכון של רשומה
        public async Task UpsertAsync(CurrencyRate rate)
        {
            var existing = await GetRateAsync(rate.Code);
            if (existing == null)
            {
                await _db.InsertAsync(rate);
            }
            else
            {
                existing.RateToILS = rate.RateToILS;
                existing.LastUpdatedUtc = rate.LastUpdatedUtc;
                await _db.UpdateAsync(existing);
            }
        }

        // מחזיר את התאריך האחרון שבו עודכן שער כלשהו
        public async Task<DateTime?> GetLastUpdatedUtcAsync()
        {
            var all = await _db.Table<CurrencyRate>().ToListAsync();
            if (all.Count == 0)
                return null;

            return all.Max(r => r.LastUpdatedUtc);
        }
    }
}
