using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.ShoppingCart
{
    public partial class ShoppingCartModel : BaseNopModel
    {
        public partial class ShoppingCartItemModel : BaseNopEntityModel
        {
            public List<ShippingCartItemModel> ShippingCartItems { get; set; }

            public decimal SubTotalValue { get; set; }
        }

        public partial class ShippingCartItemModel : BaseNopEntityModel
        {
            public string RecipientName { get; set; }

            public int ShippingCartPosition { get; set; }
        }

        public List<string> RecipientNames { get; set; } 
    }
}

public class RecipientDropdownListValue
{
    public const int SelectNewRecipientId = 0;

    public const int Myself = -1;

    public const int NewRecipient = -2;
}