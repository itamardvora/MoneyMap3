using SQLite;
using System;
using System.Linq;
using System.Threading.Tasks;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public class CurrencyRateRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public CurrencyRateRepository(SQLiteAsyncConnection db)
        {
            _db = db;
            _db.CreateTableAsync<CurrencyRate>().Wait();
        }

        public async Task<CurrencyRate> GetRateAsync(string code)
        {
            code = Normalize(code);

            return await _db.Table<CurrencyRate>()
                .Where(r => r.Code == code)
                .FirstOrDefaultAsync();
        }

        public async Task UpsertAsync(CurrencyRate rate)
        {
            if (rate == null)
                return;

            rate.Code = Normalize(rate.Code);

            if (string.IsNullOrWhiteSpace(rate.Code))
                return;

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

        public async Task<DateTime?> GetLastUpdatedUtcAsync()
        {
            var all = await _db.Table<CurrencyRate>().ToListAsync();

            if (all.Count == 0)
                return null;

            return all.Max(r => r.LastUpdatedUtc);
        }

        private static string Normalize(string code)
        {
            return (code ?? "").Trim().ToUpperInvariant();
        }
    }
}