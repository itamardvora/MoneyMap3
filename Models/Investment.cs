using SQLite;
using SQLiteNetExtensions.Attributes;
using System;

namespace MoneyMap.Models
{
    [Table("UserInvestmentsTable")]
    public class Investment
    {
        [ForeignKey(typeof(User))]
        public int UserID { get; set; }  // קישור למשתמש שביצעה את הקנייה 

        [PrimaryKey, AutoIncrement]
        public int InvestmentID { get; set; }

        [NotNull]
        public string StockSymbol { get; set; }

        [NotNull]
        public DateTime BuyDate { get; set; }

        [NotNull]
        public decimal BuyPrice { get; set; }

        [NotNull]
        public decimal Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Ignore]//  לא שומרים אדאטה בייס משהו שכל הזמןמשתנה
        public double CurrentPrice { get; set; }

        [NotNull]
        public string OriginalCurrency { get; set; } = "ILS";

        [NotNull]
        public decimal FxRateToIlsAtPurchase { get; set; } = 1m;

        // סכום העסקה בש"ח (Quantity * BuyPrice * FxRateToIlsAtPurchase)
        [NotNull]
        public decimal TotalInIls { get; set; } = 0m;

        // נתיב קובץ קבלה אופציונלי
        public string ReceiptImagePath { get; set; }// לא מצשתצמש בזה
    }
}
