using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLiteNetExtensions.Attributes;
using MoneyMap.Models;


namespace MoneyMap.Models
{
    [Table("UserInvestmentsTable")]
    public class Investment
    {


        [ForeignKey(typeof(User))]
        public int UserID { get; set; }


        [PrimaryKey, AutoIncrement]
        public int InvestmentID { get; set; } // מפתח ראשי


        [NotNull]
        public string StockSymbol { get; set; }     // סמל המניה


        [NotNull]
        public DateTime BuyDate { get; set; } // תאריך קניית המניה


        [NotNull]
        public decimal BuyPrice { get; set;} //מחיר הקנייה


        [NotNull]
        public decimal Quantity { get; set;} //כמות המניות


        public DateTime CreatedAt { get; set; } = DateTime.Now; // מתי יצר את הקטגוריות הזאת

        [Ignore] // אם אתה משתמש ב־SQLite.NET
        public double CurrentPrice { get; set; }
        public string Symbol { get; internal set; }
    }
}