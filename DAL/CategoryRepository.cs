using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public class CategoryRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public CategoryRepository(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        public async Task<int> AddCategory(string categoryName, int createdByUserID)
        {
            categoryName = (categoryName ?? "").Trim();

            var category = new Category
            {
                CategoryName = categoryName,
                CreatedByUserID = createdByUserID,
                IsSystem = false
            };

            await _db.InsertAsync(category);
            App.BackupState?.MarkCategoriesChanged();

            return category.CategoryID;
        }

        public async Task EnsureDefaultCategoriesAsync()
        {
            var defaults = new[]
            {
                "סופר",
                "מסעדות",
                "תחבורה",
                "בריאות",
                "בילויים",
                "חשבונות",
                "חיסכון"
            };

            var existingSystemCategories = await GetSystemCategories();

            foreach (var name in defaults)
            {
                bool exists = existingSystemCategories.Any(c =>
                    string.Equals(
                        (c.CategoryName ?? "").Trim(),
                        name,
                        StringComparison.OrdinalIgnoreCase));

                if (exists)
                    continue;

                var category = new Category
                {
                    CategoryName = name,
                    CreatedByUserID = 0,
                    IsSystem = true
                };

                await _db.InsertAsync(category);
            }
        }

        public async Task<List<Category>> GetCategoriesForUser(int userId)
        {
            var categories = await _db.Table<Category>()
                .Where(c => c.CreatedByUserID == userId || c.IsSystem)
                .ToListAsync();

            return categories
                .OrderBy(c => c.IsSystem ? 0 : 1)
                .ThenBy(c => c.CategoryName)
                .ToList();
        }

        public async Task<List<Category>> GetSystemCategories()
        {
            return await _db.Table<Category>()
                .Where(c => c.IsSystem)
                .ToListAsync();
        }

        public async Task DeleteCategory(int userId, int categoryId)
        {
            var category = await _db.Table<Category>()
                .Where(c => c.CategoryID == categoryId && c.CreatedByUserID == userId && !c.IsSystem)
                .FirstOrDefaultAsync();

            if (category != null)
            {
                await _db.DeleteAsync(category);
                App.BackupState?.MarkCategoriesChanged();
            }
        }
    }
}