using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using System;
using System.Threading.Tasks;
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

        public async Task<int> AddCategory(string categoryName, int createdByUserID)// הוספת קטגוריה 
        {
            var category = new Category
            {
                CategoryName = categoryName,
                CreatedByUserID = createdByUserID,
                IsSystem = false
            };

            await _db.InsertAsync(category);
            return category.CategoryID; 
        }


        public async Task<List<Category>> GetCategoriesForUser(int userId)// שולף רשימה של הקטגוריה שנוצרו על ידי המערכת ועל ידי המשתמש 
        {
            return await _db.Table<Category>()
                            .Where(c => c.CreatedByUserID == userId || c.IsSystem)
                            .ToListAsync();
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
                                    .Where(c => c.CategoryID == categoryId && c.CreatedByUserID == userId)
                                    .FirstOrDefaultAsync();

            if (category != null)
            {
                await _db.DeleteAsync(category);
            }
        }
    }
}
