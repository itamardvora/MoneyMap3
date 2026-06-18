
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

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public StockPriceService(
            StockPriceCacheRepository cache,
            string alphaVantageApiKey,
            TimeSpan cacheTtl)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _alphaKey = alphaVantageApiKey ?? throw new ArgumentNullException(nameof(alphaVantageApiKey));
            _ttl = cacheTtl <= TimeSpan.Zero ? TimeSpan.FromHours(24) : cacheTtl;
        }

        // מביא מחיר מניה עדכני או מהטבלה או מהאי פי אי 
        public async Task<decimal> GetPriceOrFetch(string symbol)
        {
            symbol = (symbol ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("יש להזין סימבול מניה");

            var cached = await _cache.GetBySymbol(symbol);

            // 1. קודם בודקים אם יש מחיר תקף במטמון
            if (cached != null &&
                cached.LastPrice > 0m &&
                (DateTime.UtcNow - cached.LastUpdated.ToUniversalTime()) <= _ttl)
            {
                return cached.LastPrice;
            }

            // 2. אם אין מטמון תקף, מנסים להביא מחיר מה-API
            try
            {
                var livePrice = await FetchFromAlphaVantage(symbol);

                if (livePrice > 0m)
                {
                    await _cache.InsertOrUpdate(symbol, livePrice);
                    return livePrice;
                }

                throw new Exception("Alpha Vantage החזיר מחיר 0 או מחיר לא תקין");
            }
            catch (Exception ex)
            {
                // 3. אם ה-API נכשל אבל יש מחיר ישן במטמון, עדיף להשתמש בו
                if (cached != null && cached.LastPrice > 0m)
                    return cached.LastPrice;

                // 4. אם אין גם מטמון — מחזירים שגיאה ברורה
                throw new Exception(
                    "לא הצלחתי להביא מחיר עבור המניה " + symbol +
                    ". פירוט: " + ex.Message
                );
            }
        }


        //שולח בקשה ומחזיר מחיר מניה עדכני מה אי פי אי
        private async Task<decimal> FetchFromAlphaVantage(string symbol)
        {
            var url =
                "https://www.alphavantage.co/query" +
                "?function=GLOBAL_QUOTE" +
                "&symbol=" + Uri.EscapeDataString(symbol) +
                "&apikey=" + Uri.EscapeDataString(_alphaKey);

            using (var response = await _http.GetAsync(url))
            {
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "שגיאת HTTP מה-API: " + response.StatusCode +
                        ". תשובת שרת: " + json
                    );
                }

                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    // Alpha Vantage מחזיר Note בדרך כלל כשעוברים מגבלת קריאות
                    if (root.TryGetProperty("Note", out var note))
                    {
                        throw new Exception("Alpha Vantage החזיר מגבלת שימוש: " + note.GetString());
                    }

                    // לפעמים מחזיר Information במקום מחיר
                    if (root.TryGetProperty("Information", out var info))
                    {
                        throw new Exception("Alpha Vantage החזיר מידע במקום מחיר: " + info.GetString());
                    }

                    // סימבול לא תקין / שגיאה
                    if (root.TryGetProperty("Error Message", out var error))
                    {
                        throw new Exception("Alpha Vantage החזיר שגיאה: " + error.GetString());
                    }

                    if (!root.TryGetProperty("Global Quote", out var quote))
                    {
                        throw new Exception("Alpha Vantage לא החזיר Global Quote. JSON: " + json);
                    }

                    if (!quote.TryGetProperty("05. price", out var priceProp))
                    {
                        throw new Exception("Alpha Vantage לא החזיר את השדה 05. price. JSON: " + json);
                    }

                    var priceText = priceProp.GetString();

                    if (decimal.TryParse(
                            priceText,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var price))
                    {
                        if (price > 0m)
                            return price;

                        throw new Exception("המחיר שחזר מה-API הוא 0");
                    }

                    throw new Exception("המחיר שחזר מה-API אינו מספר תקין: " + priceText);
                }
            }
        }
    }
}