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
    [Table("UsersTable")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int UserID { get; set; } // מפתח ראשי


        [NotNull]
        public string FullName { get; set; }     // שם מלא


        public string Password { get; set; } // סיסמה (מאובטחת בעתיד)


        [NotNull, Unique]
        public string Email { get; set; } // כתובת אימייל


        [NotNull]
        public DateTime BirthDate { get; set; } // תאריך לידה


        public DateTime CreatedAt { get; set; } = DateTime.Now; // מתי נוצר המשתמש

       public string Role { get; set; } = "User";// "Admin" או "User" לפי הצורך


        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Investment> Investments { get; set; }


        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Budget> Budget { get; set; }


        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Category> Categories { get; set; } //בדיקה למה לא בטוח אני אשאיר את זה 





    }
}