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

        public PortfolioService(InvestmentRepository investments, StockPriceService prices)
        {
            _investments = investments ?? throw new ArgumentNullException(nameof(investments));
            _prices = prices ?? throw new ArgumentNullException(nameof(prices));
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
                    if (p > 0) map[sym] = p;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PortfolioService] skip symbol {sym}: {ex.Message}");
                    // לא מפילים חישוב על כשל סימבול בודד
                }
            }
            return map;
        }

        // תאימות לשם שה-Activity שלך מצפה לו
        public Task<decimal> GetPortfolioTotalValueAsync(int userId)
            => GetTotalPortfolioValue(userId);

        // שווי תיק כולל (ILS)
        public async Task<decimal> GetTotalPortfolioValue(int userId)
        {
            var list = await _investments.GetAllByUser(userId);
            if (list.Count == 0) return 0m;

            var priceMap = await BuildPriceMap(list.Select(i => i.StockSymbol));
            decimal total = 0m;

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);
                if (!priceMap.TryGetValue(sym, out var current)) continue;
                total += current * inv.Quantity;
            }
            return total;
        }

        // רווח/הפסד כספי כולל (ILS)
        public async Task<decimal> GetPortfolioPnLAsync(int userId)
        {
            var list = await _investments.GetAllByUser(userId);
            if (list.Count == 0) return 0m;

            var priceMap = await BuildPriceMap(list.Select(i => i.StockSymbol));
            decimal totalPnl = 0m;

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);
                if (!priceMap.TryGetValue(sym, out var current)) continue;

                var cost = inv.BuyPrice * inv.Quantity;
                var currVal = current * inv.Quantity;
                totalPnl += (currVal - cost);
            }
            return totalPnl;
        }

        // מעשיר את רשימת ההשקעות במחיר נוכחי (אם קיים שדה כזה במודל)
        public async Task<List<Investment>> EnrichInvestmentsWithPrices(List<Investment> investments)
        {
            if (investments == null || investments.Count == 0)
                return new List<Investment>();

            var symbols = investments.Select(i => i.StockSymbol);
            var priceMap = await BuildPriceMap(symbols);

            foreach (var inv in investments)
            {
                var sym = Norm(inv.StockSymbol);
                if (priceMap.TryGetValue(sym, out var current))
                {
                    inv.CurrentPrice = (double)current;
                }
            }

            return investments;
        }

        // כמות והושקע לכל סימבול
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
                aggr.TotalInvested += (decimal)inv.BuyPrice * inv.Quantity;
            }
            return dict.Values.ToList();
        }

        // סיכום מלא לסימבול אחד
        public async Task<SymbolSummary> GetSummaryBySymbol(int userId, string symbol)
        {
            var sym = Norm(symbol);
            var list = await _investments.GetBySymbol(userId, sym);

            var priceMap = await BuildPriceMap(new[] { sym });
            priceMap.TryGetValue(sym, out var current);

            decimal invested = 0m;
            decimal qty = 0m;
            foreach (var inv in list)
            {
                invested += (decimal)inv.BuyPrice * inv.Quantity;
                qty += inv.Quantity;
            }

            var currentValue = current * qty;
            var profit = currentValue - invested;
            var ret = invested == 0 ? 0m : profit / invested;

            return new SymbolSummary
            {
                Symbol = sym,
                TotalQuantity = qty,
                TotalInvested = invested,
                CurrentPrice = current,
                CurrentValue = currentValue,
                Profit = profit,
                Return = ret
            };
        }

        // התפלגות אחוזית משווי התיק לכל סימבול
        public async Task<List<BreakdownEntry>> GetPortfolioBreakdown(int userId)
        {
            var list = await _investments.GetAllByUser(userId);
            if (list.Count == 0) return new List<BreakdownEntry>();

            var priceMap = await BuildPriceMap(list.Select(i => i.StockSymbol));
            var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var inv in list)
            {
                var sym = Norm(inv.StockSymbol);
                if (!priceMap.TryGetValue(sym, out var current)) continue;

                var add = current * inv.Quantity;
                values[sym] = values.TryGetValue(sym, out var v) ? v + add : add;
            }

            var total = values.Values.Sum();
            if (total == 0)
                return values.Select(kv => new BreakdownEntry { Symbol = kv.Key, Percent = 0.0 }).ToList();

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

    // DTOs
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
        public decimal Return { get; set; } // 0.12 = 12%
    }

    public class BreakdownEntry
    {
        public string Symbol { get; set; }
        public double Percent { get; set; }
    }
}
