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
    public class BudgetStatus // מחזיקה נתנוים מהבדגט סרוויס
    {
        public int BudgetID { get; set; }
        public int CategoryID { get; set; }
        public decimal MonthlyLimit { get; set; }
        public decimal Spent { get; set; }
        public decimal Remaining { get; set; }
        public double UsagePct { get; set; }
        public string CategoryName { get; set; }

    }
}