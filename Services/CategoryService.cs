using System.Collections.Generic;
using System.Threading.Tasks;
using MoneyMap.DAL;
using MoneyMap.Models;

namespace MoneyMap.Services
{
    public class CategoryService
    {
        private readonly CategoryRepository _categoryRepository;

        public CategoryService(CategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task EnsureDefaultCategoriesAsync()
        {
            await _categoryRepository.EnsureDefaultCategoriesAsync();
        }

        public async Task<int> AddCategory(string name, int createdByUserId)
        {
            return await _categoryRepository.AddCategory(name, createdByUserId);
        }

        public async Task<List<Category>> GetCategoriesForUser(int userId)
        {
            return await _categoryRepository.GetCategoriesForUser(userId);
        }

        public async Task<List<Category>> GetSystemCategories()
        {
            return await _categoryRepository.GetSystemCategories();
        }

        public async Task DeleteCategory(int userId, int categoryId)
        {
            await _categoryRepository.DeleteCategory(userId, categoryId);
        }
    }
}