using Nop.Core.Configuration;

namespace Nop.Core.Domain.Orders
{
    public partial class OrderSettings : ISettings
    {
        public bool ShippingCheckout {
            get { return false; }
        }
    }
}