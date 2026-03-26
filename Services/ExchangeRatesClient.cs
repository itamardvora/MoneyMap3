using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoneyMap.Services
{
    /// מחזיר שערים עדכניים יחסית ל-ILS עבור סט קודים מבוקש.
    /// מתבסס על CurrencyApiClient שמושך שערים מבנק ישראל.
    public class ExchangeRatesClient : IExchangeRatesClient
    {
        private readonly CurrencyApiClient _api;

        public ExchangeRatesClient(CurrencyApiClient api = null)
        {
            _api = api ?? new CurrencyApiClient();
        }

        public async Task<Dictionary<string, decimal>> GetLatestAsync(IEnumerable<string> baseCurrencies)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["ILS"] = 1.0m
            };

            // נמשוך רק מה שביקשו בפועל
            foreach (var code in baseCurrencies)
            {
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                if (string.Equals(code, "ILS", StringComparison.OrdinalIgnoreCase))
                {
                    result["ILS"] = 1.0m;
                    continue;
                }

                try
                {
                    switch (code.ToUpperInvariant())
                    {
                        case "USD":
                            result["USD"] = await _api.GetUsdToIlsAsync();
                            break;
                        case "EUR":
                            result["EUR"] = await _api.GetEurToIlsAsync();
                            break;
                        default:
                            // קוד לא נתמך כרגע  אל תכשיל את כל הבקשה
                            break;
                    }
                }
                catch
                {
                    // במקרה כישלון רשת/פורמט – לא נזרוק חריגה כללית; ניתן לשכבה מעל ליפול למטמון.
                }
            }

            return result;
        }
    }
}
