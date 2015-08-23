using System;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Web.Framework.Mvc;
using Nop.Web.Models.Common;

namespace Nop.Web.Models.Checkout
{
    public partial class CheckoutShippingAddressModel : BaseNopModel
    {
        public CheckoutShippingAddressModel()
        {
            ExistingAddresses = new List<AddressModel>();
            NewAddress = new AddressModel();
        }

        public IList<AddressModel> ExistingAddresses { get; set; }

        public AddressModel NewAddress { get; set; }

        public bool NewAddressPreselected { get; set; }

        public bool AllowPickUpInStore { get; set; }
        public string PickUpInStoreFee { get; set; }
        public bool PickUpInStore { get; set; }

        public ShippingCart ShippingCart { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string ExpectedDeliveryPeriod { get; set; }

        public string GreetingType { get; set; }

        public string To { get; set; }

        public string From { get; set; }

        public string Message { get; set; }
    }
}