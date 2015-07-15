using System.Collections.Generic;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders
{
    public partial interface IOrderShippingService
    {
        OrderShipping GetOrderShippingById(int orderShippingId);

        void UpdateOrderShipping(OrderShipping orderShipping);
    }
}
