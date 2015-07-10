using System;
using System.Collections.Generic;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;

namespace Nop.Core.Domain.Orders
{
    public class OrderShipping : BaseEntity
    {
        public int ShippingAddressId { get; set; }

        public virtual Address ShippingAddress { get; set; }


        public int OrderId { get; set; }

        public virtual Order Order { get; set; }


        public int ShippingStatusId { get; set; }

        public ShippingStatus ShippingStatus
        {
            get
            {
                return (ShippingStatus)this.ShippingStatusId;
            }
            set
            {
                this.ShippingStatusId = (int)value;
            }
        }

        public bool PickUpInStore { get; set; }

        public string ShippingMethod { get; set; }

        public string ShippingRateComputationMethodSystemName { get; set; }

        private ICollection<Shipment> _shipments;
        public virtual ICollection<Shipment> Shipments
        {
            get { return _shipments ?? (_shipments = new List<Shipment>()); }
            protected set { _shipments = value; }
        }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string RecipientName { get; set; }

        private ICollection<OrderShippingItem> _orderShippingItems { get; set; }

        public ICollection<OrderShippingItem> OrderShippingItems
        {
            get { return _orderShippingItems ?? (new List<OrderShippingItem>()); }
            set { _orderShippingItems = value; }
        }
    }
}
