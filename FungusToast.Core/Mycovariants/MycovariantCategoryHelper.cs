using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    /// <summary>
    /// Static helper class providing utility methods for working with mycovariant categories.
    /// </summary>
    public static class MycovariantCategoryHelper
    {
        /// <summary>
        /// Returns a list of mycovariant IDs that belong to the specified category.
        /// </summary>
        /// <param name="category">The mycovariant category to filter by.</param>
        /// <returns>A list of mycovariant IDs in the specified category.</returns>
        public static List<int> GetPreferredMycovariantIds(MycovariantCategory category)
        {
            return MycovariantRepository.All
                .Where(m => m.Category == category)
                .Select(m => m.Id)
                .ToList();
        }

        /// <summary>
        /// Returns a list of mycovariant IDs that belong to any of the specified categories.
        /// </summary>
        /// <param name="categories">The mycovariant categories to filter by.</param>
        /// <returns>A list of mycovariant IDs in any of the specified categories.</returns>
        public static List<int> GetPreferredMycovariantIds(params MycovariantCategory[] categories)
        {
            if (categories == null || categories.Length == 0)
                return new List<int>();

            var categorySet = categories.ToHashSet();
            return MycovariantRepository.All
                .Where(m => categorySet.Contains(m.Category))
                .Select(m => m.Id)
                .ToList();
        }

        /// <summary>
        /// Returns all mycovariants grouped by their categories.
        /// </summary>
        /// <returns>A dictionary mapping categories to lists of mycovariant IDs.</returns>
        public static Dictionary<MycovariantCategory, List<int>> GetMycovariantIdsByCategory()
        {
            return MycovariantRepository.All
                .GroupBy(m => m.Category)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(m => m.Id).ToList()
                );
        }

        /// <summary>
        /// Returns the category of a specific mycovariant by its ID.
        /// </summary>
        /// <param name="mycovariantId">The ID of the mycovariant to look up.</param>
        /// <returns>The category of the mycovariant, or null if not found.</returns>
        public static MycovariantCategory? GetCategoryById(int mycovariantId)
        {
            var mycovariant = MycovariantRepository.All.FirstOrDefault(m => m.Id == mycovariantId);
            return mycovariant?.Category;
        }

        /// <summary>
        /// Returns all available mycovariant categories that have at least one mycovariant assigned.
        /// </summary>
        /// <returns>A list of categories that contain mycovariants.</returns>
        public static List<MycovariantCategory> GetUsedCategories()
        {
            return MycovariantRepository.All
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}