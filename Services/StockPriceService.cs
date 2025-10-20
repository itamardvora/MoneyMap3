using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MoneyMap.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using MoneyMap.DAL;


namespace MoneyMap.Services
{
    public class StockPriceService
    {
        private readonly StockPriceCacheRepository _cache;
        private readonly TimeSpan _ttl;     // כמה זמן cache תקף
        private readonly string _alphaKey;  // מפתח ה-API

        // ttl ברירת מחדל: יום אחד. אפשר לשנות מבחוץ אם תרצה.
        public StockPriceService(StockPriceCacheRepository cache, string alphaVantageApiKey, TimeSpan? ttl = null)
        {
            _cache = cache;
            _alphaKey = alphaVantageApiKey;
            _ttl = ttl ?? TimeSpan.FromDays(1);
        }

        public async Task<decimal?> GetPrice(string symbol)
        {
            var c = await _cache.GetBySymbol(symbol);
            return c?.LastPrice;
        }

        public Task UpdateCache(string symbol, decimal price)
        {
            return _cache.InsertOrUpdate(symbol, price);
        }

        public async Task<bool> IsCacheValid(string symbol)
        {
            var c = await _cache.GetBySymbol(symbol);
            if (c == null) return false;
            return (DateTime.Now - c.LastUpdated) < _ttl;
        }

        public async Task<decimal> GetPriceOrFetch(string symbol)
        {
            // 1) אם יש Cache תקף (פחות מיום) – מחזירים ממנו
            if (await IsCacheValid(symbol))
            {
                var cached = await _cache.GetBySymbol(symbol);
                return cached.LastPrice;
            }

            // 2) אחרת – מביאים מחיר אמיתי מה-API
            var fresh = await AlphaVantageClient.GetPriceAsync(symbol, _alphaKey);
            if (!fresh.HasValue)
                throw new InvalidOperationException("נכשלה הבאת מחיר מה-API");

            // 3) מעדכנים Cache ומחזירים
            await UpdateCache(symbol, fresh.Value);
            return fresh.Value;
        }


    }
}