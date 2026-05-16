using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MoneyMap.DAL;
using MoneyMap.Models;
using Xamarin.Essentials;

namespace MoneyMap.Services
{
    public class CurrencyService
    {
        private readonly CurrencyRateRepository _ratesRepo;
        private readonly IExchangeRatesClient _client;
        private readonly TimeSpan _ttl;

      

        public CurrencyService(
      CurrencyRateRepository ratesRepo,
      IExchangeRatesClient client = null,
      TimeSpan? ttl = null)
        {
            _ratesRepo = ratesRepo;
            _client = client;
            _ttl = ttl ?? TimeSpan.FromHours(24);
        }

        public async Task EnsureBaseRatesAsync()
        {
            await RefreshRatesIfNeededAsync(new[] { "USD", "EUR" });
        }

        public async Task RefreshRatesIfNeededAsync(IEnumerable<string> baseCurrencies)
        {
            if (_client == null)
                return;

            var wanted = (baseCurrencies ?? new List<string>())
                .Select(Norm)
                .Where(c => c != "" && c != "ILS")
                .Distinct()
                .ToList();

            if (wanted.Count == 0)
                return;

            bool needRefresh = false;

            foreach (var code in wanted)
            {
                var existing = await _ratesRepo.GetRateAsync(code);

                if (existing == null ||
                    existing.RateToILS <= 0m ||
                    existing.LastUpdatedUtc == default(DateTime) ||
                    DateTime.UtcNow - existing.LastUpdatedUtc.ToUniversalTime() > _ttl)
                {
                    needRefresh = true;
                    break;
                }
            }

            if (!needRefresh)
                return;

            var live = await _client.GetLatestAsync(wanted);

            foreach (var kv in live)
            {
                var code = Norm(kv.Key);

                if (code == "" || kv.Value <= 0m)
                    continue;

                await _ratesRepo.UpsertAsync(new CurrencyRate
                {
                    Code = code,
                    RateToILS = kv.Value,
                    LastUpdatedUtc = DateTime.UtcNow
                });
            }
        }

        public async Task<decimal> ConvertAsync(decimal amount, string fromCode, string toCode)
        {
            fromCode = Norm(fromCode);
            toCode = Norm(toCode);

            if (amount == 0m || fromCode == toCode)
                return amount;

            decimal fromToIls = 1m;
            decimal toToIls = 1m;

            if (fromCode != "ILS")
                fromToIls = await GetRateToIlsPreferDbAsync(fromCode);

            if (toCode != "ILS")
                toToIls = await GetRateToIlsPreferDbAsync(toCode);

            decimal amountInIls = amount * fromToIls;

            if (toCode == "ILS")
                return amountInIls;

            return amountInIls / toToIls;
        }

     

        public async Task<string> FormatAsync(decimal amount, string fromCode, string toCode, int decimals = 2)
        {
            toCode = Norm(toCode);

            var converted = await ConvertAsync(amount, fromCode, toCode);
            var symbol = GetCurrencySymbol(toCode);

            var formatted = Math.Round(converted, decimals)
                .ToString("N" + decimals, CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(symbol))
                return symbol + formatted;

            return formatted + " " + toCode;
        }

        public async Task<decimal?> GetRateAsync(string fromCode, string toCode)
        {
            fromCode = Norm(fromCode);
            toCode = Norm(toCode);

            if (fromCode == toCode)
                return 1m;

            try
            {
                decimal fromToIls = 1m;
                decimal toToIls = 1m;

                if (fromCode != "ILS")
                    fromToIls = await GetRateToIlsPreferDbAsync(fromCode);

                if (toCode != "ILS")
                    toToIls = await GetRateToIlsPreferDbAsync(toCode);

                return fromToIls / toToIls;
            }
            catch
            {
                return null;
            }
        }

        private async Task<decimal> GetRateToIlsPreferDbAsync(string code)
        {
            code = Norm(code);

            if (code == "ILS")
                return 1m;

            var existing = await _ratesRepo.GetRateAsync(code);

            if (existing != null && existing.RateToILS > 0m)
                return existing.RateToILS;

            await TryRefreshRatesSafeAsync(new[] { code });

            existing = await _ratesRepo.GetRateAsync(code);

            if (existing != null && existing.RateToILS > 0m)
                return existing.RateToILS;

            throw new Exception("לא נמצא שער מטבע תקין עבור " + code);
        }

        private async Task TryRefreshRatesSafeAsync(IEnumerable<string> codes)
        {
            try
            {
                await RefreshRatesIfNeededAsync(codes);
            }
            catch
            {
                // לא מפילים את האפליקציה בגלל בנק ישראל/אינטרנט.
                // אם אחרי זה אין שער בטבלה, מי שקרא לפונקציה יקבל שגיאה ברורה.
            }
        }

        private static string Norm(string code)
        {
            return (code ?? "").Trim().ToUpperInvariant();
        }

        private static string GetCurrencySymbol(string code)
        {
            code = Norm(code);

            switch (code)
            {
                case "ILS": return "₪";
                case "USD": return "$";
                case "EUR": return "€";
                case "GBP": return "£";
                case "JPY": return "¥";
                default: return "";
            }
        }
    }
}