using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.GiaHelper
{
    public class ModelBuilder
    {
        public static IList<int> BuildCategoriesTree(IRepository<Category> categoryRepository, int rootCategoryId)
        {
            var list = new List<int> { rootCategoryId };

            var childrenId = from c in categoryRepository.Table where c.ParentCategoryId == rootCategoryId select c.Id;
            foreach (var childId in childrenId)
            {
                list = GetChildrenOfCategory(categoryRepository, childId, list);
            }

            return list;
        }

        private static List<int> GetChildrenOfCategory(IRepository<Category> categoryRepository, int categoryId, IList<int> categoryIds)
        {
            categoryIds.Add(categoryId);

            var childrenId = from c in categoryRepository.Table where c.ParentCategoryId == categoryId select c.Id;
            foreach (var childId in childrenId)
            {
                categoryIds.Add(childId);
            }

            return categoryIds.ToList();
        }
    }
}
