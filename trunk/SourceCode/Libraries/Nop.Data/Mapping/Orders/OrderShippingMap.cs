using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public class OrderShippingMap : NopEntityTypeConfiguration<OrderShipping>
    {
        public OrderShippingMap()
        {
            this.ToTable("OrderShipping");
            this.HasKey(o => o.Id);

            this.Ignore(o => o.ShippingStatus);

            this.HasRequired(o => o.Order)
                .WithMany(o => o.OrderShippings)
                .HasForeignKey(o => o.OrderId);

            this.HasOptional(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId);
        }
    }
}
