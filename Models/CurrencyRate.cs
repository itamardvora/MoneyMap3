using SQLite;
using System;

namespace MoneyMap.Models
{
    [Table("CurrencyRates")]
    public class CurrencyRate
    {
        [PrimaryKey]
        public string Code { get; set; } // לדוגמה: "ILS", "USD", "EUR"

        [NotNull]
        public decimal RateToILS { get; set; }// שער המרה
       

        [NotNull]
        public DateTime LastUpdatedUtc { get; set; }
        // מתי השער עודכן לאחרונה (UTC)
    }
}
