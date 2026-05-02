using SQLite;
using SQLiteNetExtensions.Attributes;
using System;

namespace MoneyMap.Models
{
    [Table("UserInvestmentsTable")]
    public class Investment
    {
        [ForeignKey(typeof(User))]
        public int UserID { get; set; }

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

        [Ignore]// משהו שכל הזמןמשתנה
        public double CurrentPrice { get; set; }

        // --- מטבע מקור של הקנייה (ILS / USD / EUR). ברירת מחדל ILS.
        [NotNull]
        public string OriginalCurrency { get; set; } = "ILS";

        // כמה ILS ל-1 OriginalCurrency בתאריך הקנייה (שער היסטורי)
        [NotNull]
        public decimal FxRateToIlsAtPurchase { get; set; } = 1m;

        // סכום העסקה בש"ח (Quantity * BuyPrice * FxRateToIlsAtPurchase)
        [NotNull]
        public decimal TotalInIls { get; set; } = 0m;

        // נתיב קובץ קבלה (אופציונלי)
        public string ReceiptImagePath { get; set; }
    }
}
