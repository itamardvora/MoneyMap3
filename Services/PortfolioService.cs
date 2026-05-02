
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

                summary.TotalCostIls += costIls;
                summary.TotalValueIls += currentValueIls;
                summary.TotalProfitIls += currentValueIls - costIls;
            }

            summary.ReturnPercent = summary.TotalCostIls == 0m
                ? 0m
                : (summary.TotalProfitIls / summary.TotalCostIls) * 100m;

            return summary;
        }

        public async Task<decimal> GetPortfolioTotalValueAsync(int userId)
        {
            var summary = await GetPortfolioSummaryAsync(userId);
            return summary.TotalValueIls;
        }

        public Task<decimal> GetTotalPortfolioValue(int userId)
            => GetPortfolioTotalValueAsync(userId);

        public async Task<decimal> GetPortfolioPnLAsync(int userId)
        {
            var summary = await GetPortfolioSummaryAsync(userId);
            return summary.TotalProfitIls;
        }

        public async Task<decimal> GetPortfolioTotalCostAsync(int userId)
        {
            var summary = await GetPortfolioSummaryAsync(userId);
            return summary.TotalCostIls;
        }

        public async Task<decimal> GetPortfolioReturnPercentAsync(int userId)
        {
            var summary = await GetPortfolioSummaryAsync(userId);
            return summary.ReturnPercent;
        }

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

            if (list == null)
                return dict.Values.ToList();

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);

                if (!dict.TryGetValue(sym, out var aggr))
                {
                    aggr = new SymbolAggregate { Symbol = sym };
                    dict[sym] = aggr;
                }

                aggr.TotalQuantity += inv.Quantity;
                aggr.TotalInvested += GetCostIls(inv);
            }

            return dict.Values.ToList();
        }

        public async Task<SymbolSummary> GetSummaryBySymbol(int userId, string symbol)
        {
            var sym = Norm(symbol);
            var list = await _investments.GetBySymbol(userId, sym);

            decimal investedIls = 0m;
            decimal qty = 0m;

            if (list != null)
            {
                foreach (var inv in list)
                {
                    investedIls += GetCostIls(inv);
                    qty += inv.Quantity;
                }
            }

            var priceMap = await BuildPriceMap(new[] { sym });
            priceMap.TryGetValue(sym, out var currentPriceUsd);

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

    public class PortfolioSummary
    {
        public decimal TotalValueIls { get; set; }
        public decimal TotalCostIls { get; set; }
        public decimal TotalProfitIls { get; set; }
        public decimal ReturnPercent { get; set; }
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

        public decimal CurrentPrice { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal Profit { get; set; }
        public decimal Return { get; set; }
    }

    public class BreakdownEntry
    {
        public string Symbol { get; set; }
        public double Percent { get; set; }
    }
}