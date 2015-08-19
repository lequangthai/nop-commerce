using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.EasyPay2
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //statusHandler
            routes.MapRoute("Plugin.Payments.EasyPay2.statusHandler",
                 "Plugins/PaymentEasyPay2/statusHandler",
                 new { controller = "PaymentEasyPay2", action = "statusHandler" },
                 new[] { "Nop.Plugin.Payments.EasyPay2.Controllers" }
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
