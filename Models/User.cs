using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;

namespace MoneyMap.Models
{
    [Table("UsersTable")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int UserID { get; set; }

        [NotNull]
        public string FullName { get; set; }

        public string Password { get; set; }

        [NotNull, Unique]
        public string Email { get; set; }

        [Unique]
        public string FirebaseUid { get; set; }

        [NotNull]
        public DateTime BirthDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Role { get; set; } = "User";

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Investment> Investments { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Budget> Budget { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        public List<Category> Categories { get; set; }
    }
}