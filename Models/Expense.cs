// Models/Expense.cs
using SQLite;
using SQLiteNetExtensions.Attributes;
using System;

namespace MoneyMap.Models
{
    [Table("ExpensesTable")]
    public class Expense
    {
        [PrimaryKey, AutoIncrement] public int ExpenseID { get; set; }

        [ForeignKey(typeof(User))] public int UserID { get; set; }
        [ForeignKey(typeof(Category))] public int CategoryID { get; set; }

        [NotNull] public decimal Amount { get; set; }
        [NotNull] public DateTime Date { get; set; } = DateTime.Now;
        [NotNull] public string Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ✅ השדה היחיד לשמירת קבלה
        public string ReceiptPath { get; set; }

        // קשרים (אופציונלי)
        [ManyToOne] public User User { get; set; }
        [ManyToOne] public Category Category { get; set; }
    }
}
