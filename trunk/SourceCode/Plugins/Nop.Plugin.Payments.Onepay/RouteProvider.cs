using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.Onepay
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //PDT
            routes.MapRoute("Plugin.Payments.Onepay.PDTHandler",
                 "Plugins/PaymentOnepay/PDTHandler",
                 new { controller = "PaymentOnepay", action = "PDTHandler" },
                 new[] { "Nop.Plugin.Payments.Onepay.Controllers" }
            );
            //IPN
            routes.MapRoute("Plugin.Payments.Onepay.IPNHandler",
                 "Plugins/PaymentOnepay/IPNHandler",
                 new { controller = "PaymentOnepay", action = "IPNHandler" },
                 new[] { "Nop.Plugin.Payments.Onepay.Controllers" }
            );
            //Cancel
            routes.MapRoute("Plugin.Payments.Onepay.CancelOrder",
                 "Plugins/PaymentOnepay/CancelOrder",
                 new { controller = "PaymentOnepay", action = "CancelOrder" },
                 new[] { "Nop.Plugin.Payments.Onepay.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
