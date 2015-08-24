using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    public partial class OrderShippingRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapLocalizedRoute("NEXT_RECIPIENT",
                            "checkout/RecepientAddress/{recepientId}",
                            new { controller = "Checkout", action = "ShippingAddress", recepientId = UrlParameter.Optional },
                            new { recepientId = @"\d+" },
                            new[] { "Nop.Web.Controllers" });

            routes.MapLocalizedRoute("REVIEW_SHIPPINGORDER_ACTION",
                            "checkout/ShippingAddressesReview/",
                            new { controller = "ShoppingCart", action = "ShippingOrderReview" },
                            new[] { "Nop.Web.Controllers" });

            routes.MapLocalizedRoute("PAYMENT_PROCESS",
                            "checkout/PaymentProcess/",
                            new { controller = "Checkout", action = "OrderBillingAddress" },
                            new[] { "Nop.Web.Controllers" });


        }

        public int Priority {
            get { return 0; }
        }
    }
}
