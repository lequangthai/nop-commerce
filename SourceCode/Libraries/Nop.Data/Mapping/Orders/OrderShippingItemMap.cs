using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public class OrderShippingItemMap : NopEntityTypeConfiguration<OrderShippingItem>
    {
        public OrderShippingItemMap()
        {
            this.ToTable("OrderShipping_OrderItem_Mapping");
            this.HasKey(o => o.Id);

            this.HasRequired(osi => osi.OrderItem)
                .WithMany(oi=>oi.OrderShippingItems)
                .HasForeignKey(osi => osi.OrderItemId)
                .WillCascadeOnDelete(false);

            this.HasRequired(osi => osi.OrderShipping)
                .WithMany(os => os.OrderShippingItems)
                .HasForeignKey(osi => osi.OrderShippingId)
                .WillCascadeOnDelete(false);
        }
    }
}
