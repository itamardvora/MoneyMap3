using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MoneyMap.DAL;
using MoneyMap.Models;
using Xamarin.Essentials;

namespace MoneyMap.Services
    // אחראי על כל ההמרות מטבעות שקל דולר יורו ואומר האם המטבעות מעודכנים 
{
    public class CurrencyService
    {
        private readonly UserSettingsRepository _userSettings;
        private readonly CurrencyRateRepository _ratesRepo;
        private readonly IExchangeRatesClient _client;
        private readonly TimeSpan _ttl;

        private const string PrefKeyPreferredCurrency = "preferred_currency_code";
        private const string DefaultCurrency = "ILS";

        public CurrencyService(UserSettingsRepository userSettings,
                               CurrencyRateRepository ratesRepo,
                               IExchangeRatesClient client = null,
                               TimeSpan? ttl = null)
        {
            _userSettings = userSettings;
            _ratesRepo = ratesRepo;
            _client = client;
            _ttl = ttl ?? TimeSpan.FromHours(24);
        }

       
        /// טוען/מרענן את המטבעות הבסיסיים שאנחנו צריכים לאפליקציה.
     
   
        public async Task EnsureBaseRatesAsync()
        {
            var wanted = new[] { "USD", "EUR" };
            await RefreshRatesIfStaleAsync(wanted);
        }

   
        /// אם עבר TTL מאז העדכון האחרון – מושך "לייב" ומעדכן ב-DB.
      
        public async Task RefreshRatesIfStaleAsync(IEnumerable<string> baseCurrencies)
        {
            var last = await _ratesRepo.GetLastUpdatedUtcAsync();
            if (last.HasValue && (DateTime.UtcNow - last.Value.ToUniversalTime()) < _ttl)
                return;

            if (_client == null) return; // בלי לקוח רשת – אין ריענון

            var live = await _client.GetLatestAsync(baseCurrencies);
            foreach (var kv in live)
            {
                await _ratesRepo.UpsertAsync(new CurrencyRate
                {
                    Code = kv.Key,
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

            decimal fromToIls = 1m, ilsToTo = 1m;

            if (fromCode != "ILS")
            {
                var r = await _ratesRepo.GetRateAsync(fromCode);
                fromToIls = r?.RateToILS ?? 1m;
            }

            if (toCode != "ILS")
            {
                var r = await _ratesRepo.GetRateAsync(toCode);
                ilsToTo = (r is null || r.RateToILS == 0m) ? 1m : 1m / r.RateToILS;
            }

            return amount * fromToIls * ilsToTo;
        }

        /// עיצוב סכום לפי המטבע המועדף.
        
        public async Task<string> FormatAmountForDisplayAsync(decimal amount, string amountCurrencyCode, int decimals = 2)
        {
            var preferred = await GetPreferredCurrencyCodeAsync();
            return await FormatAsync(amount, amountCurrencyCode, preferred, decimals);
        }

        public async Task<string> FormatAsync(decimal amount, string fromCode, string toCode, int decimals = 2)
        {
            var converted = await ConvertAsync(amount, fromCode, toCode);
            var symbol = GetCurrencySymbol(toCode);
            var formatted = Math.Round(converted, decimals)
                                .ToString($"N{decimals}", CultureInfo.InvariantCulture);
            return !string.IsNullOrEmpty(symbol)
                ? $"{symbol}{formatted}"
                : $"{formatted} {Norm(toCode)}";
        }


        /// שליפת יחס המרה ישיר בין שני מטבעות (אם קיים בטבלה).

        public async Task<decimal?> GetRateAsync(string fromCode, string toCode)
        {
            fromCode = Norm(fromCode);
            toCode = Norm(toCode);

            if (fromCode == toCode)
                return 1m;

            decimal fromToIls = 1m;
            decimal toToIls = 1m;

            if (fromCode != "ILS")
            {
                var rFrom = await _ratesRepo.GetRateAsync(fromCode);

                if (rFrom == null || rFrom.RateToILS <= 0m)
                    return null;

                fromToIls = rFrom.RateToILS;
            }

            if (toCode != "ILS")
            {
                var rTo = await _ratesRepo.GetRateAsync(toCode);

                if (rTo == null || rTo.RateToILS <= 0m)
                    return null;

                toToIls = rTo.RateToILS;
            }

            return fromToIls / toToIls;
        }


        // --- ניהול המטבע המועדף ---
        public Task<string> GetPreferredCurrencyCodeAsync()
        {
            var code = Preferences.Get(PrefKeyPreferredCurrency, DefaultCurrency);
            return Task.FromResult(Norm(code));
        }

        public Task SetPreferredCurrencyAsync(string code)
        {
            Preferences.Set(PrefKeyPreferredCurrency, Norm(code));
            return Task.CompletedTask;
        }

        // --- עזרים ---
        private static string Norm(string code) =>
            (code ?? "").Trim().ToUpperInvariant();

        private static string GetCurrencySymbol(string code)
        {
            code = Norm(code);
            return code switch
            {
                "ILS" => "₪",
                "USD" => "$",
                "EUR" => "€",
                "GBP" => "£",
                "JPY" => "¥",
                _ => ""
            };
        }
    }
}
