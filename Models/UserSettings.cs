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

namespace MoneyMap.Models
{
    [Table("UserSettings")]
    public class UserSettings
    {
        [PrimaryKey]
        public int Id { get; set; } = 1; // תמיד שורה יחידה

        [NotNull]
        public string DisplayCurrencyCode { get; set; } = "ILS"; // "ILS" / "USD" / "EUR"
    }
}
