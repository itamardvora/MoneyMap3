using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public class UserRepository
    {
        private readonly SQLiteAsyncConnection _db;
        private static bool _schemaEnsured = false; // דגל פנימי שמוודא שהטבלה והאינדקסים נוצרו פעם אחת בלבד

        public UserRepository(SQLiteAsyncConnection db)
        {
            _db = db;
            _ = EnsureSchemaAsync();
        }


        // אחראית לוודא שטבלת המשתמשים קיימת ומעודכנת עם כל העמודות והאינדקסים הדרושים
        private async Task EnsureSchemaAsync()
        {
            if (_schemaEnsured) return;

            await _db.CreateTableAsync<User>();

            try
            {
                await _db.ExecuteAsync("ALTER TABLE UsersTable ADD COLUMN FirebaseUid TEXT");
            }
            catch
            {
            }

            await _db.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS idx_users_email ON UsersTable (Email)");
            await _db.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS idx_users_firebaseuid ON UsersTable (FirebaseUid)");

            _schemaEnsured = true;
        }

        public Task AddUser(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
                user.Email = user.Email.Trim().ToLowerInvariant();

            return _db.InsertAsync(user);
        }

        public Task UpdateUser(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
                user.Email = user.Email.Trim().ToLowerInvariant();

            return _db.UpdateAsync(user);
        }

        public Task<User> GetUserByEmail(string email)
        {
            if (!string.IsNullOrWhiteSpace(email))
                email = email.Trim().ToLowerInvariant();

            return _db.Table<User>()
                      .Where(u => u.Email == email)
                      .FirstOrDefaultAsync();
        }

        public Task<User> GetUserById(int userId)
        {
            return _db.Table<User>()
                      .Where(u => u.UserID == userId)
                      .FirstOrDefaultAsync();
        }

        public Task<User> GetUserByFirebaseUid(string firebaseUid)
        {
            return _db.Table<User>()
                      .Where(u => u.FirebaseUid == firebaseUid)
                      .FirstOrDefaultAsync();
        }

        public Task<List<User>> GetAllUsers()
        {
            return _db.Table<User>()
                      .OrderBy(u => u.FullName)
                      .ToListAsync();
        }

        public async Task UpdateFullNameAsync(int userId, string fullName)
        {
            var user = await GetUserById(userId);
            if (user == null) return;

            user.FullName = fullName?.Trim();
            await _db.UpdateAsync(user);
        }

        public Task<int> DeleteUserByIdAsync(int userId)
        {
            return _db.Table<User>()
                      .Where(u => u.UserID == userId)
                      .DeleteAsync();
        }

        
    }
}