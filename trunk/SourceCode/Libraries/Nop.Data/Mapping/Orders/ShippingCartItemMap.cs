using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public class ShippingCartItemMap : NopEntityTypeConfiguration<ShippingCartItem>
    {
        public ShippingCartItemMap()
        {
            this.ToTable("ShippingCart_ShippingCartItem_Mapping");
            this.HasKey(o => o.Id);

            this.HasRequired(s => s.ShippingCart)
                .WithMany(o => o.ShippingCartItems)
                .HasForeignKey(s => s.ShippingCartId);

            this.HasRequired(osi => osi.ShoppingCartItem)
                .WithMany()
                .HasForeignKey(osi => osi.ShoppingCartItemId)
                .WillCascadeOnDelete(false);
        }
    }
}
