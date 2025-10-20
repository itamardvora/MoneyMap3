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
    [Table("ExpensesTable")]
    public class Expense
    {

        [PrimaryKey, AutoIncrement]
        public int ExpenseID { get; set; }


        [ForeignKey(typeof(User))]
        public int UserID { get; set; }


        [ForeignKey(typeof(Category))]
        public int CategoryID { get; set; }


        [NotNull]
        public decimal Amount { get; set; }


        [NotNull]
        public DateTime Date { get; set; } = DateTime.Now;


        [NotNull]
        public string Reason { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // קשרים 
        [ManyToOne]
        public User User { get; set; }

        [ManyToOne]
        public Category Category { get; set; }

    }
}