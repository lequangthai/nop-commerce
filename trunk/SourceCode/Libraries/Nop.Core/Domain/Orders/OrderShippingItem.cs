using System;
using System.Collections.Generic;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;

namespace Nop.Core.Domain.Orders
{
    public class OrderShippingItem : BaseEntity
    {
        public int OrderShippingId { get; set; }

        public virtual OrderShipping OrderShipping { get; set; }

        public int OrderItemId { get; set; }

        public virtual OrderItem OrderItem { get; set; }

        public int Quantity { get; set; }
    }
}
