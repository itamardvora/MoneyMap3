using SQLite;
using SQLiteNetExtensions.Attributes;
using System;

namespace MoneyMap.Models
{
    [Table("IncomesTable")]
    public class Income
    {
        [PrimaryKey, AutoIncrement]
        public int IncomeID { get; set; }

        [ForeignKey(typeof(User))]
        public int UserID { get; set; }

        [NotNull]
        public decimal Amount { get; set; }

        [NotNull]
        public DateTime Date { get; set; } = DateTime.Now;

        [NotNull]
        public string Source { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ManyToOne]
        public User User { get; set; }
    }
}