using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoneyMap.Models
{
    public class DisplayBudgetStatus
    {
        public int CategoryID { get; set; }          // מזהה הקטגוריה (בשביל לדעת מאיזו קטגוריה לפתוח הוצאות)
        public string CategoryName { get; set; }

        public string PlannedText { get; set; }    // למשל "$1,000.00"
        public string SpentText { get; set; }      // למשל "$250.00"
        public string RemainingText { get; set; }  // למשל "$750.00"
        public int Progress { get; set; }          // 0..100
    }
}
