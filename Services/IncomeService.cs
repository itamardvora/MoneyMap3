using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MoneyMap.DAL;
using MoneyMap.Models;

namespace MoneyMap.Services
{
    public class IncomeService
    {
        private readonly IncomeRepository _incomeRepository;

        public IncomeService(IncomeRepository incomeRepository)
        {
            _incomeRepository = incomeRepository;
        }

        public async Task AddIncome(int userId, decimal amount, string source, DateTime date)
        {
            if (userId <= 0)
                throw new Exception("משתמש לא תקין");

            if (amount <= 0)
                throw new Exception("סכום ההכנסה חייב להיות גדול מ-0");

            if (string.IsNullOrWhiteSpace(source))
                throw new Exception("יש להזין מקור הכנסה");

            var income = new Income
            {
                UserID = userId,
                Amount = amount,
                Source = source.Trim(),
                Date = date,
                CreatedAt = DateTime.Now
            };

            await _incomeRepository.AddIncome(income);
        }

        public Task<List<Income>> GetMonthlyIncomes(int userId, DateTime month)
        {
            return _incomeRepository.GetUserIncomes(userId, month);
        }

        public Task<decimal> GetTotalIncomeByMonth(int userId, DateTime month)
        {
            return _incomeRepository.GetTotalIncomeByMonth(userId, month);
        }

        public Task DeleteIncome(int userId, int incomeId)
        {
            return _incomeRepository.DeleteIncome(userId, incomeId);
        }
    }
}