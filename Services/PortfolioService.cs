// MoneyMap/Services/PortfolioService.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MoneyMap.DAL;
using MoneyMap.Models;

namespace MoneyMap.Services
{
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
        }

        private async Task<decimal> ConvertUsdToIlsAsync(decimal amountUsd)
        {
            if (amountUsd == 0m)
                return 0m;

            if (_currency == null)
                return amountUsd;

            try
            {
                return await _currency.ConvertAsync(amountUsd, "USD", "ILS");
            }
            catch
            {
                return amountUsd;
            }
        }

        public Task<decimal> GetPortfolioTotalValueAsync(int userId)
            => GetTotalPortfolioValue(userId);

        // שווי תיק כולל בשקלים
        public async Task<decimal> GetTotalPortfolioValue(int userId)
        {
            var list = await _investments.GetAllByUser(userId);
            if (list == null || list.Count == 0)
                return 0m;

            var priceMap = await BuildPriceMap(list.Select(i => i.StockSymbol));
            decimal totalIls = 0m;

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);

                if (!priceMap.TryGetValue(sym, out var currentPriceUsd))
                    continue;

                var currentValueUsd = currentPriceUsd * inv.Quantity;
                var currentValueIls = await ConvertUsdToIlsAsync(currentValueUsd);

                totalIls += currentValueIls;
            }

            return totalIls;
        }

        // רווח/הפסד כולל בשקלים
        public async Task<decimal> GetPortfolioPnLAsync(int userId)
        {
            var list = await _investments.GetAllByUser(userId);
            if (list == null || list.Count == 0)
                return 0m;

            var priceMap = await BuildPriceMap(list.Select(i => i.StockSymbol));
            decimal totalPnlIls = 0m;

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);

                if (!priceMap.TryGetValue(sym, out var currentPriceUsd))
                    continue;

                var costIls = inv.BuyPrice * inv.Quantity;

                var currentValueUsd = currentPriceUsd * inv.Quantity;
                var currentValueIls = await ConvertUsdToIlsAsync(currentValueUsd);

                totalPnlIls += currentValueIls - costIls;
            }

            return totalPnlIls;
        }

        // מעשיר השקעות במחיר נוכחי בדולר
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

        public async Task<List<SymbolAggregate>> GetAggregatedBySymbol(int userId)
        {
            var list = await _investments.GetAllByUser(userId);

            var dict = new Dictionary<string, SymbolAggregate>(StringComparer.OrdinalIgnoreCase);

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);

                if (!dict.TryGetValue(sym, out var aggr))
                {
                    aggr = new SymbolAggregate { Symbol = sym };
                    dict[sym] = aggr;
                }

                aggr.TotalQuantity += inv.Quantity;
                aggr.TotalInvested += inv.BuyPrice * inv.Quantity;
            }

            return dict.Values.ToList();
        }

        public async Task<SymbolSummary> GetSummaryBySymbol(int userId, string symbol)
        {
            var sym = Norm(symbol);
            var list = await _investments.GetBySymbol(userId, sym);

            var priceMap = await BuildPriceMap(new[] { sym });
            priceMap.TryGetValue(sym, out var currentPriceUsd);

            decimal investedIls = 0m;
            decimal qty = 0m;

            foreach (var inv in list)
            {
                investedIls += inv.BuyPrice * inv.Quantity;
                qty += inv.Quantity;
            }

            var currentValueUsd = currentPriceUsd * qty;
            var currentValueIls = await ConvertUsdToIlsAsync(currentValueUsd);

            var profitIls = currentValueIls - investedIls;
            var ret = investedIls == 0m ? 0m : profitIls / investedIls;

            return new SymbolSummary
            {
                Symbol = sym,
                TotalQuantity = qty,
                TotalInvested = investedIls,
                CurrentPrice = currentPriceUsd,
                CurrentValue = currentValueIls,
                Profit = profitIls,
                Return = ret
            };
        }

        public async Task<List<BreakdownEntry>> GetPortfolioBreakdown(int userId)
        {
            var list = await _investments.GetAllByUser(userId);
            if (list == null || list.Count == 0)
                return new List<BreakdownEntry>();

            var priceMap = await BuildPriceMap(list.Select(i => i.StockSymbol));
            var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);

                if (!priceMap.TryGetValue(sym, out var currentPriceUsd))
                    continue;

                var valueUsd = currentPriceUsd * inv.Quantity;
                var valueIls = await ConvertUsdToIlsAsync(valueUsd);

                values[sym] = values.TryGetValue(sym, out var existing)
                    ? existing + valueIls
                    : valueIls;
            }

            var total = values.Values.Sum();

            if (total == 0m)
            {
                return values
                    .Select(kv => new BreakdownEntry { Symbol = kv.Key, Percent = 0.0 })
                    .ToList();
            }

            return values
                .Select(kv => new BreakdownEntry
                {
                    Symbol = kv.Key,
                    Percent = Math.Round((double)(kv.Value / total) * 100.0, 2)
                })
                .OrderByDescending(x => x.Percent)
                .ToList();
        }

        public Task<List<Investment>> GetBySymbol(int userId, string stockSymbol)
            => _investments.GetBySymbol(userId, Norm(stockSymbol));
    }

    public class SymbolAggregate
    {
        public string Symbol { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalInvested { get; set; }
    }

    public class SymbolSummary
    {
        public string Symbol { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalInvested { get; set; }

        // מחיר מניה נוכחי בדולר
        public decimal CurrentPrice { get; set; }

        // שווי נוכחי בשקלים
        public decimal CurrentValue { get; set; }

        // רווח/הפסד בשקלים
        public decimal Profit { get; set; }

        // 0.12 = 12%
        public decimal Return { get; set; }
    }

    public class BreakdownEntry
    {
        public string Symbol { get; set; }
        public double Percent { get; set; }
    }
}