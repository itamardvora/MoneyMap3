using SQLite;
using System.Linq;
using System.Threading.Tasks;
using System;

// מוודאת שטבלת ההשקעות מעודכנת ומכילה את כל העמודות הדרושות לגרסה הנוכחית של האפליקציה
// תפקיד המחלקה לראות שהטבלת השקעות עודמת בשינוייים ובנויה כמו שצריך 
namespace MoneyMap.DAL

{
    public class InvestmentMigrationHelper
    {
        private readonly SQLiteAsyncConnection _db;
        private const string TableName = "UserInvestmentsTable";

        public InvestmentMigrationHelper(SQLiteAsyncConnection db) => _db = db;

        public async Task EnsureInvestmentCurrencyColumnsAsync() // מוודאת שכל העמודות הדרושות קיימות בטבלת ההשקעות
        {
            // שולף את רשימת העמודות הקיימות בטבלה
            var cols = await _db.QueryAsync<TableInfoRow>($"PRAGMA table_info({TableName})");

            // מוסיפה עמודה חדשה לטבלה אם היא עדיין לא קיימת
            async Task EnsureColAsync(string name, string type, string defaultSql = null)
            {
                if (!cols.Any(c => string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    var sql = $"ALTER TABLE {TableName} ADD COLUMN {name} {type}";
                    if (!string.IsNullOrWhiteSpace(defaultSql))
                        sql += $" DEFAULT {defaultSql}";
                    await _db.ExecuteAsync(sql);
                }
            }


            // מוסיף עמודה למטבע המקורי של ההשקעה
            await EnsureColAsync("OriginalCurrency", "TEXT", "'ILS'");
            await EnsureColAsync("FxRateToIlsAtPurchase", "REAL", "1");
            await EnsureColAsync("TotalInIls", "REAL", "0");
            await EnsureColAsync("ReceiptImagePath", "TEXT", "NULL");//לא בשימוש
            await _db.ExecuteAsync($"CREATE INDEX IF NOT EXISTS idx_invest_user ON {TableName}(UserID)");
            await _db.ExecuteAsync($"CREATE INDEX IF NOT EXISTS idx_invest_symbol ON {TableName}(StockSymbol)");
        }

        private class TableInfoRow
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int notnull { get; set; }
            public string dflt_value { get; set; }
            public int pk { get; set; }
        }
    }
}
