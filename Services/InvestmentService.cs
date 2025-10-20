using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MoneyMap.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using MoneyMap.DAL;



namespace MoneyMap.Services
{
    public class InvestmentService
    {
        private readonly InvestmentRepository _repo;
        private readonly StockPriceService _stockPriceService;


        public InvestmentService(InvestmentRepository repo, StockPriceService stockPriceService)
        {
            _repo = repo;
            _stockPriceService = stockPriceService;

        }

        public async Task AddInvestment(int userId, string symbol, DateTime buyDate, int buyPrice, decimal quantity)
        {
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("symbol required");
            if (quantity <= 0) throw new ArgumentException("quantity must be positive");
            if (buyPrice <= 0) throw new ArgumentException("buyPrice must be positive");
            if (buyDate > DateTime.Now.AddMinutes(1)) throw new ArgumentException("buyDate cannot be in the future");

            var inv = new Investment
            {
                UserID = userId,
                StockSymbol = symbol.Trim().ToUpperInvariant(),
                BuyDate = buyDate,
                BuyPrice = buyPrice,      // במודל שלך int
                Quantity = quantity,
                CreatedAt = DateTime.Now
            };

            await _repo.AddInvestment(inv);
        }

        public Task DeleteInvestment(int userId, int investmentId)
        {
            return _repo.DeleteInvestment(investmentId, userId);
        }

        public Task<Investment> GetInvestmentById(int userId, int investmentId)
        {
            return _repo.GetById(investmentId, userId);
        }

        public async Task<decimal> CalculateProfitAsync(Investment inv)
        {
            var currentPrice = await _stockPriceService.GetPriceOrFetch(inv.StockSymbol);
            var buy = (decimal)inv.BuyPrice;
            return (currentPrice - buy) * inv.Quantity;
        }

        public async Task<decimal> CalculateReturnAsync(Investment inv)
        {
            var currentPrice = await _stockPriceService.GetPriceOrFetch(inv.StockSymbol);
            var buy = (decimal)inv.BuyPrice;
            if (buy == 0) return 0m;
            return (currentPrice - buy) / buy; // יחסי: 0.12 = 12%
        }


    }
}