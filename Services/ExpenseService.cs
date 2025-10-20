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
    public class ExpenseService
    {
        private readonly ExpenseRepository _expenseRepo;

        public ExpenseService(ExpenseRepository expenseRepo)
        {
            _expenseRepo = expenseRepo;
        }

        public async Task AddExpense(int userId, int categoryId, decimal amount, string reason, DateTime date)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be positive");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason is required");

            var expense = new Expense
            {
                UserID = userId,
                CategoryID = categoryId,
                Amount = amount,
                Reason = reason.Trim(),
                Date = date.Date,
                CreatedAt = DateTime.Now
            };

            await _expenseRepo.AddExpense(expense);
        }

        public Task<List<Expense>> GetMonthlyExpenses(int userId, DateTime month)
        {
            return _expenseRepo.GetUserExpenses(userId, month);
        }

        public Task<List<ExpenseCategoryTotal>> GetExpensesByCategory(int userId, DateTime month)
        {
            return _expenseRepo.GetExpensesByCategory(userId, month);
        }
        //public async Task<Dictionary<int, decimal>> GetExpensesByCategory(int userId, DateTime month)
        //{
        //    var list = await _expenseRepo.GetExpensesByCategory(userId, month);
        //    return list.ToDictionary(x => x.CategoryID, x => x.TotalAmount);
        //}

        public async Task<List<(int CategoryID, double Percentage)>> GetCategoryBreakdown(int userId, DateTime month)
        {
            var categoryTotals = await _expenseRepo.GetExpensesByCategory(userId, month);
            var total = categoryTotals.Sum(x => x.TotalAmount);

            var result = new List<(int CategoryID, double Percentage)>();

            foreach (var item in categoryTotals)
            {
                var percent = total > 0 ? Math.Round((double)(item.TotalAmount / total) * 100, 2) : 0;
                result.Add((item.CategoryID, percent));
            }

            return result;
        }

       

        public Task DeleteExpense(int userId, int expenseId)
        {
            return _expenseRepo.DeleteExpense(userId, expenseId);
        }

    }
}