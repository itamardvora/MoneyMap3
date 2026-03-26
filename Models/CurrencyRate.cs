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
        public decimal RateToILS { get; set; }
        // כמה ₪ שווה יחידה אחת של המטבע הזה
        // לדוגמה: USD: 3.8 => 1 USD = 3.8 ₪

        [NotNull]
        public DateTime LastUpdatedUtc { get; set; }
        // מתי השער עודכן לאחרונה (UTC)
    }
}
