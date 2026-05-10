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
        private readonly StockPriceService _stockPriceService;
        private readonly CurrencyService _currencyService;

        public InvestmentService(InvestmentRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _stockPriceService = null;
            _currencyService = null;
        }

        public InvestmentService(InvestmentRepository repo, StockPriceService stockPriceService)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _stockPriceService = stockPriceService;
            _currencyService = null;
        }

        public InvestmentService(
            InvestmentRepository repo,
            StockPriceService stockPriceService,
            CurrencyService currencyService)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _stockPriceService = stockPriceService;
            _currencyService = currencyService;
        }

        private static string Norm(string s)
        {
            return (s ?? string.Empty).Trim().ToUpperInvariant();
        }

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

            if (userId <= 0)
                throw new ArgumentException("משתמש לא תקין");

            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("סימבול חובה");

            if (quantity <= 0)
                throw new ArgumentException("כמות חייבת להיות גדולה מ-0");

            if (buyPrice <= 0)
                throw new ArgumentException("מחיר קנייה חייב להיות גדול מ-0");

            if (buyDate.Date > DateTime.Today)
                throw new ArgumentException("אי אפשר לבחור תאריך עתידי");

            if (originalCurrency != "ILS" && originalCurrency != "USD" && originalCurrency != "EUR")
                throw new ArgumentException("מטבע לא תקין");

            decimal fxToIlsAtPurchase = 1m;

            /*
             * חשוב:
             * השמירה לא תלויה במחיר מניה נוכחי.
             * כלומר לא קוראים כאן ל-StockPriceService.
             * הסיבה: אם ה-API איטי / לא עובד / עבר מגבלה,
             * עדיין צריך שההשקעה תישמר ותופיע במסך.
             */

            if (originalCurrency != "ILS")
            {
                if (_currencyService != null)
                {
                    try
                    {
                        var rate = await _currencyService.GetRateAsync(originalCurrency, "ILS");

                        if (rate.HasValue && rate.Value > 0m)
                            fxToIlsAtPurchase = rate.Value;
                    }
                    catch
                    {
                        // לא מפילים שמירה בגלל שער מטבע.
                        // אם אין שער, נשמור עם 1 כדי שההשקעה לא תיעלם.
                        fxToIlsAtPurchase = 1m;
                    }
                }
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

        public Task AddInvestment(int userId, string symbol, DateTime buyDate, int buyPrice, decimal quantity)
        {
            return AddInvestment(userId, symbol, quantity, buyPrice, buyDate, "ILS");
        }

        public Task DeleteInvestment(int userId, int investmentId)
        {
            return _repo.DeleteInvestment(investmentId, userId);
        }

        public Task<Investment> GetInvestmentById(int userId, int investmentId)
        {
            return _repo.GetById(investmentId, userId);
        }

        public Task<List<Investment>> GetInvestmentsForUserAsync(int userId)
        {
            return _repo.GetAllByUser(userId);
        }

        public async Task<decimal> CalculateProfitAsync(Investment inv)
        {
            if (inv == null)
                return 0m;

            if (_stockPriceService == null)
                return 0m;

            decimal currentPriceUsd;

            try
            {
                currentPriceUsd = await _stockPriceService.GetPriceOrFetch(Norm(inv.StockSymbol));
            }
            catch
            {
                return 0m;
            }

            var currentValueUsd = currentPriceUsd * inv.Quantity;

            decimal currentValueIls = currentValueUsd;

            if (_currencyService != null)
            {
                try
                {
                    currentValueIls = await _currencyService.ConvertAsync(currentValueUsd, "USD", "ILS");
                }
                catch
                {
                    currentValueIls = currentValueUsd;
                }
            }

            decimal costIls;

            if (inv.TotalInIls > 0m)
                costIls = inv.TotalInIls;
            else
                costIls = inv.BuyPrice * inv.Quantity * Math.Max(inv.FxRateToIlsAtPurchase, 1m);

            return currentValueIls - costIls;
        }

        public async Task<decimal> CalculateReturnAsync(Investment inv)
        {
            if (inv == null)
                return 0m;

            var pnl = await CalculateProfitAsync(inv);

            decimal costIls;

            if (inv.TotalInIls > 0m)
                costIls = inv.TotalInIls;
            else
                costIls = inv.BuyPrice * inv.Quantity * Math.Max(inv.FxRateToIlsAtPurchase, 1m);

            if (costIls == 0m)
                return 0m;

            return pnl / costIls;
        }
    }
}