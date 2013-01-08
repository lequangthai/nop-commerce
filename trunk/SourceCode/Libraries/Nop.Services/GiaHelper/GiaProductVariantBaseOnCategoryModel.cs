using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.GiaHelper
{
    public class GiaProductVariantBaseOnCategoryModel
    {
        public string CategoryName { get; set; }

        public int CategoryId { get;set; }

        public IList<ProductVariant> ProductVariants { get; set; }
    }
}
