using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public class OrderShippingItemMap : NopEntityTypeConfiguration<OrderShippingItem>
    {
        public OrderShippingItemMap()
        {
            this.ToTable("OrderShipping_OrderItem_Mapping");
            this.HasKey(o => o.Id);

            this.HasRequired(s => s.OrderShipping)
                .WithMany(o => o.OrderShippingItems)
                .HasForeignKey(s => s.OrderShippingId);

            this.HasRequired(osi => osi.OrderItem)
                .WithMany()
                .HasForeignKey(osi => osi.OrderItemId)
                .WillCascadeOnDelete(false);
        }
    }
}
