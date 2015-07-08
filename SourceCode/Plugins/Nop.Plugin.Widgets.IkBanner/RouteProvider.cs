using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Widget.Banner
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.Widgets.IkBanner.AddPopup",
                 "Plugins/WidgetsIkBanner/AddPopup",
                 new { controller = "WidgetsIkBanner", action = "AddPopup" },
                 new[] { "Nop.Plugin.Widgets.IkBanner.Controllers" }
            );
            routes.MapRoute("Plugin.Widgets.IkBanner.EditPopup",
                 "Plugins/WidgetsIkBanner/EditPopup",
                 new { controller = "WidgetsIkBanner", action = "EditPopup" },
                 new[] { "Nop.Plugin.Widgets.IkBanner.Controllers" }
            );

            routes.MapRoute("Plugin.Widgets.IkBanner.AddWidgetPopup",
                 "Plugins/WidgetsIkBanner/AddWidgetPopup",
                 new { controller = "WidgetsIkBanner", action = "AddWidgetPopup" },
                 new[] { "Nop.Plugin.Widgets.IkBanner.Controllers" }
            );
            routes.MapRoute("Plugin.Widgets.IkBanner.EditWidgetPopup",
                 "Plugins/WidgetsIkBanner/EditWidgetPopup",
                 new { controller = "WidgetsIkBanner", action = "EditWidgetPopup" },
                 new[] { "Nop.Plugin.Widgets.IkBanner.Controllers" }
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
