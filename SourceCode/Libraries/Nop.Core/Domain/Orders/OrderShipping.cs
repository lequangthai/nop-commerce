using System;
using System.Collections.Generic;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;

namespace Nop.Core.Domain.Orders
{
    public class OrderShipping : BaseEntity
    {
        public int? ShippingAddressId { get; set; }

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

        private ICollection<OrderShippingItem> _orderShippingItems;
        public virtual ICollection<OrderShippingItem> OrderShippingItems
        {
            get { return _orderShippingItems ?? ( _orderShippingItems = new List<OrderShippingItem>()); }
            protected set { _orderShippingItems = value; }
        }

        private ICollection<Shipment> _shipments;
        public virtual ICollection<Shipment> Shipments
        {
            get { return _shipments ?? (_shipments = new List<Shipment>()); }
            protected set { _shipments = value; }
        }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string ExpectedDeliveryPeriod { get; set; }

        public string Title { get; set; }

        public string RecipientName { get; set; }


        public string GreetingType { get; set; }

        public string To { get; set; }

        public string From { get; set; }

        public string Message { get; set; }

        public decimal ShippingFee { get; set; }
    }
}
