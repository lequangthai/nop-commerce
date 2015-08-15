namespace Nop.Core.Domain.Orders
{
    public class ShippingCartItem : BaseEntity
    {
        public int ShippingCartId { get; set; }

        public virtual ShippingCart ShippingCart { get; set; }

        public int ShoppingCartItemId { get; set; }

        public virtual ShoppingCartItem ShoppingCartItem { get; set; }

        public int ShippingCartPosition { get; set; }
    }
}
