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
    [Table("UserBudgetTable")]
    public class Budget
    {
        [PrimaryKey, AutoIncrement]
        public int BudgetID { get; set; }


        [ForeignKey(typeof(User))]
        public int UserID { get; set; }


        [ForeignKey(typeof(Category))]
        public int CategoryID { get; set; }


        [NotNull]
        public decimal MonthlyLimit { get; set; }


        [NotNull]
        public DateTime BudgetMonth { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);


        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ManyToOne]
        public User User { get; set; }

        [ManyToOne]
        public Category Category { get; set; }


     
    }

}