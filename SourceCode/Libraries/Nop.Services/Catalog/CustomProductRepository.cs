using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.Catalog
{
    public class CustomProductRepository : EfRepository<Product>, IRepository<Product>
    {
        public CustomProductRepository(IDbContext dbContext):base(dbContext) { }

        public override IQueryable<Product> Table
        {
            get { return base.Table.Where(x=>x.StockQuantity>x.MinStockQuantity); }
        }
    }
}
