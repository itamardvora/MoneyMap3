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
    //Category
    [Table("CategoriesTable")]
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int CategoryID { get; set; }


        [NotNull]
        public string CategoryName { get; set; }  // למשל "אוכל", "תחבורה"


        [NotNull]
        public bool IsSystem { get; set; } = false;  // האם זו קטגוריית מערכת


        [ForeignKey(typeof(User))]
        public int? CreatedByUserID { get; set; }  // null אם זה של מערכת
    }
}