using System.Collections.Generic;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.ShoppingCart
{
    public partial class OrderTotalsModel : BaseNopModel
    {
        public decimal OrderTotalValue { get; set; }
    }
}