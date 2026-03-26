using System.Threading.Tasks;
using SQLite;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public class UserSettingsRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public UserSettingsRepository(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        public async Task<UserSettings> GetAsync()
        {
            var row = await _db.Table<UserSettings>().Where(x => x.Id == 1).FirstOrDefaultAsync();
            if (row == null)
            {
                row = new UserSettings { Id = 1, DisplayCurrencyCode = "ILS" };
                await _db.InsertAsync(row);
            }
            return row;
        }

        public async Task UpsertAsync(UserSettings settings)
        {
            if (settings == null) return;
            // תמיד נשמור ב-Id=1
            settings.Id = 1;
            var existing = await _db.Table<UserSettings>().Where(x => x.Id == 1).FirstOrDefaultAsync();
            if (existing == null)
                await _db.InsertAsync(settings);
            else
                await _db.UpdateAsync(settings);
        }
    }
}
