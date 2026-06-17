
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MoneyMap.DAL;
using MoneyMap.Models;

namespace MoneyMap.Services
{
    // שירות לחישוב וניתוח תיק השקעות: שווי, עלות, רווח ותשואה כוללת לפי מחירי שוק ושערי מטבע.
    public class PortfolioService
    {
        private readonly InvestmentRepository _investments;
        private readonly StockPriceService _prices;
        private readonly CurrencyService _currency;

        public PortfolioService(
            InvestmentRepository investments,
            StockPriceService prices,
            CurrencyService currency)
        {
            _investments = investments ?? throw new ArgumentNullException(nameof(investments));
            _prices = prices ?? throw new ArgumentNullException(nameof(prices));
            _currency = currency;
        }

        private static string Norm(string s) => (s ?? "").Trim().ToUpperInvariant();


        // מחשב את עלות ההשקעה בשקלים לפי מחיר קנייה ושער מטבע בזמן הרכישה
        private decimal GetCostIls(Investment inv)
        {
            if (inv == null)
                return 0m;

            if (inv.TotalInIls > 0m)
                return inv.TotalInIls;

            var fx = inv.FxRateToIlsAtPurchase > 0m
                ? inv.FxRateToIlsAtPurchase
                : 1m;

            return inv.BuyPrice * inv.Quantity * fx;
        }



       
        private async Task<Dictionary<string, decimal>> BuildPriceMap(IEnumerable<string> symbols)
        {
            var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in symbols.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var sym = Norm(s);

                try
                {
                    var p = await _prices.GetPriceOrFetch(sym);
                    if (p > 0)
                        map[sym] = p;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PortfolioService] skip symbol {sym}: {ex.Message}");
                }
            }

            return map;
        } // מביא מחיר עדכני של מניה 


        // ממיר ערך מדולרים לשקלים באמצעות CurrencyService
        private async Task<decimal?> ConvertUsdToIlsAsync(decimal amountUsd)
        {
            if (amountUsd == 0m)
                return 0m;

            if (_currency == null)
                return null;

            try
            {
                var converted = await _currency.ConvertAsync(amountUsd, "USD", "ILS");

                if (converted <= 0m)
                    return null;

                return converted;
            }
            catch
            {
                return null;
            }
        }

        // מחשב סיכום כולל של תיק ההשקעות: שווי, עלות, רווח ותשואה באחוזים
        public async Task<PortfolioSummary> GetPortfolioSummaryAsync(int userId)
        {
            var summary = new PortfolioSummary();

            var list = await _investments.GetAllByUser(userId);

            if (list == null || list.Count == 0)
                return summary;

            var priceMap = await BuildPriceMap(list.Select(i => i.StockSymbol));

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);

                if (!priceMap.TryGetValue(sym, out var currentPriceUsd))
                    continue;

                var costIls = GetCostIls(inv);

                var currentValueUsd = currentPriceUsd * inv.Quantity;
                var currentValueIls = await ConvertUsdToIlsAsync(currentValueUsd);

                if (!currentValueIls.HasValue)
                    continue;

                summary.TotalCostIls += costIls;
                summary.TotalValueIls += currentValueIls.Value;
                summary.TotalProfitIls += currentValueIls.Value - costIls;
            }

            summary.ReturnPercent = summary.TotalCostIls == 0m
                ? 0m
                : (summary.TotalProfitIls / summary.TotalCostIls) * 100m;

            return summary;
        }


        // מוסיף לכל השקעה את מחיר השוק הנוכחי של המניה
        public async Task<List<Investment>> EnrichInvestmentsWithPrices(List<Investment> investments)
        {
            if (investments == null || investments.Count == 0)
                return new List<Investment>();

            var symbols = investments.Select(i => i.StockSymbol);
            var priceMap = await BuildPriceMap(symbols);

            foreach (var inv in investments)
            {
                var sym = Norm(inv.StockSymbol);

                if (priceMap.TryGetValue(sym, out var currentPriceUsd))
                    inv.CurrentPrice = (double)currentPriceUsd;
            }

            return investments;
        }

       
    }

    public class PortfolioSummary
    {
        public decimal TotalValueIls { get; set; }
        public decimal TotalCostIls { get; set; }
        public decimal TotalProfitIls { get; set; }
        public decimal ReturnPercent { get; set; }
    }

   
}