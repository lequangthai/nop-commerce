using System;
using System.Collections.Generic;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;

namespace Nop.Core.Domain.Orders
{
    public class ShippingCart : BaseEntity
    {
        public int? ShippingAddressId { get; set; }

        public virtual Address ShippingAddress { get; set; }

        
        public int CustomerId { get; set; }

        public virtual Customer Customer { get; set; }

        public bool PickUpInStore { get; set; }

        public string ShippingMethod { get; set; }

        public string ShippingRateComputationMethodSystemName { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string RecipientName { get; set; }


        private ICollection<ShippingCartItem> _shippingCartItems;
        public virtual ICollection<ShippingCartItem> ShippingCartItems
        {
            get { return _shippingCartItems ?? (_shippingCartItems = new List<ShippingCartItem>()); }
            protected set { _shippingCartItems = value; }
        }
    }
}
