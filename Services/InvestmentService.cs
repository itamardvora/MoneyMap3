using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MoneyMap.DAL;
using MoneyMap.Models;

namespace MoneyMap.Services
{
    public class InvestmentService
    {
        private readonly InvestmentRepository _repo;
        private readonly StockPriceService _stockPriceService; // יכול להיות null
        private readonly CurrencyService _currencyService;     // יכול להיות null

        // בנאי קצר – תאימות לאזורים שקוראים רק עם repo
        public InvestmentService(InvestmentRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _stockPriceService = null;
            _currencyService = null;
        }

        // בנאי ביניים – אם יש לך שירות מחירים אבל אין CurrencyService
        public InvestmentService(InvestmentRepository repo, StockPriceService stockPriceService)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _stockPriceService = stockPriceService;
            _currencyService = null;
        }

        // בנאי מלא – אם יש גם CurrencyService
        public InvestmentService(InvestmentRepository repo, StockPriceService stockPriceService, CurrencyService currencyService)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _stockPriceService = stockPriceService;
            _currencyService = currencyService;
        }

        private static string Norm(string s) => (s ?? string.Empty).Trim().ToUpperInvariant();

        public async Task AddInvestment(
            int userId,
            string symbol,
            decimal quantity,
            decimal buyPrice,
            DateTime buyDate,
            string originalCurrency = "ILS")
        {
            symbol = Norm(symbol);
            originalCurrency = Norm(originalCurrency);
            if (userId <= 0) throw new ArgumentException("userId invalid");
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("symbol required");
            if (quantity <= 0) throw new ArgumentException("quantity must be positive");
            if (buyPrice <= 0) throw new ArgumentException("buyPrice must be positive");
            if (buyDate > DateTime.UtcNow.AddMinutes(1)) throw new ArgumentException("buyDate cannot be in the future");
            if (originalCurrency != "ILS" && originalCurrency != "USD" && originalCurrency != "EUR")
                throw new ArgumentException("originalCurrency must be ILS/USD/EUR");

            // כאן לא משנים סכמת DB – רק ממלאים את השדות שכבר קיימים במודל
            decimal fxToIlsAtPurchase = 1m;
            // אם תוסיף בעתיד CurrencyService – נחשב המרה. אחרת נשאיר 1.
            if (originalCurrency != "ILS" && _currencyService != null)
            {
                var rate = await _currencyService.GetRateAsync(originalCurrency, "ILS");
                fxToIlsAtPurchase = rate ?? 1m;
            }

            var inv = new Investment
            {
                UserID = userId,
                StockSymbol = symbol,
                BuyDate = buyDate,
                BuyPrice = buyPrice,
                Quantity = quantity,
                CreatedAt = DateTime.Now,
                OriginalCurrency = originalCurrency,
                FxRateToIlsAtPurchase = fxToIlsAtPurchase,
                TotalInIls = quantity * buyPrice * fxToIlsAtPurchase
            };

            await _repo.AddInvestment(inv);
        }

        // עטיפה לחתימה היסטורית
        public Task AddInvestment(int userId, string symbol, DateTime buyDate, int buyPrice, decimal quantity)
            => AddInvestment(userId, symbol, quantity, buyPrice, buyDate, "ILS");

        public Task DeleteInvestment(int userId, int investmentId)
            => _repo.DeleteInvestment(investmentId, userId);

        public Task<Investment> GetInvestmentById(int userId, int investmentId)
            => _repo.GetById(investmentId, userId);

        public Task<List<Investment>> GetInvestmentsForUserAsync(int userId)
            => _repo.GetAllByUser(userId);

        // פונקציות תשואה/רווח – ישתמשו בעתיד אם תרצה
        public async Task<decimal> CalculateProfitAsync(Investment inv)
        {              
            if (inv == null) return 0m;
            if (_stockPriceService == null) return 0m;

            var currentPriceIls = await _stockPriceService.GetPriceOrFetch(Norm(inv.StockSymbol));
            var currentValueIls = currentPriceIls * inv.Quantity;

            var costIls = inv.TotalInIls > 0 ? inv.TotalInIls : inv.BuyPrice * inv.Quantity;
            return currentValueIls - costIls;
        }

        public async Task<decimal> CalculateReturnAsync(Investment inv)
        {
            var pnl = await CalculateProfitAsync(inv);
            var costIls = inv.TotalInIls > 0 ? inv.TotalInIls : inv.BuyPrice * inv.Quantity;
            if (costIls == 0) return 0m;
            return pnl / costIls;
        }
    }
}
