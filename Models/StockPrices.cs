using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoneyMap.Models
{
    //StockPrices
    [Table("StockPricesCache")]
    public class StockPrices
    { 
        [PrimaryKey]
        public string StockSymbol { get; set; }  // למשל "AAPL", "TSLA", 

        public decimal LastPrice { get; set; }   // המחיר האחרון הידוע

        public DateTime LastUpdated { get; set; } = DateTime.Now;  // מתי נבדק לאחרונה
    }
}