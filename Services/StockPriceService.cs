using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MoneyMap.DAL;

namespace MoneyMap.Services
{
    public class StockPriceService
    {
        private readonly StockPriceCacheRepository _cache;
        private readonly string _alphaKey;
        private readonly TimeSpan _ttl;
        private static readonly HttpClient _http = new HttpClient();

        public StockPriceService(StockPriceCacheRepository cache, string alphaVantageApiKey, TimeSpan cacheTtl)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _alphaKey = alphaVantageApiKey ?? throw new ArgumentNullException(nameof(alphaVantageApiKey));
            _ttl = cacheTtl <= TimeSpan.Zero ? TimeSpan.FromHours(24) : cacheTtl;
        }

        public async Task<decimal> GetPriceOrFetch(string symbol)
        {
            symbol = (symbol ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol required");

            // 1) נסה מטמון תקף
            var cached = await _cache.GetBySymbol(symbol);
            if (cached != null && (DateTime.UtcNow - cached.LastUpdated.ToUniversalTime()) <= _ttl)
                return cached.LastPrice;

            // 2) נסה להביא מהאינטרנט
            try
            {
                var live = await FetchFromAlphaVantage(symbol);
                if (live > 0)
                {
                    await _cache.InsertOrUpdate(symbol, live);
                    return live;
                }
            }
            catch
            {
                // נופלים חזרה למטמון
            }

            // 3) אם אין אינטרנט/מגבלת API – תחזיר מטמון גם אם ישן
            if (cached != null)
                return cached.LastPrice;

            // 4) אין כלום – זו כבר שגיאה
            throw new InvalidOperationException($"Price not available for symbol '{symbol}'.");
        }

        private async Task<decimal> FetchFromAlphaVantage(string symbol)
        {
            // פינג קצר: GLOBAL_QUOTE (הכי חסכוני)
            var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_alphaKey}";
            using var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("Global Quote", out var quote))
                return 0m;

            if (!quote.TryGetProperty("05. price", out var priceProp))
                return 0m;

            var str = priceProp.GetString();
            if (decimal.TryParse(str, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                return price;
            }
            return 0m;
        }
    }
}
