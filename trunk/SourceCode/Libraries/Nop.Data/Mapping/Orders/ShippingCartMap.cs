using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public class ShippingCartMap : NopEntityTypeConfiguration<ShippingCart>
    {
        public ShippingCartMap()
        {
            this.ToTable("ShippingCart");
            this.HasKey(o => o.Id);

            this.HasRequired(o => o.Customer)
                .WithMany(o => o.ShippingCarts)
                .HasForeignKey(o => o.CustomerId);

            this.HasOptional(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId);
        }
    }
}
