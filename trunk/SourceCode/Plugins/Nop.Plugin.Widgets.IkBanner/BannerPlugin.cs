using System.Collections.Generic;
using System.IO;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Plugins;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Plugin.Widgets.IkBanner.Data;
using Nop.Plugin.Widgets.IkBanner.Domain;
using System.Text;
using Nop.Plugin.Widgets.IkBanner.Services;

namespace Nop.Plugin.Widgets.IkBanner
{
    public class BannerPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly IkBannerObjectContext _objectContext;
        private readonly IBannerService _bannerService;

        public BannerPlugin(IkBannerObjectContext objectContext, IBannerService bannerService)
        {
            this._objectContext = objectContext;
            this._bannerService = bannerService;
        }

        #region Methods

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "WidgetsIkBanner";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Widgets.IkBanner.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public IList<string> GetWidgetZones()
        {
            var bannerPlacements = _bannerService.GetAllBannerWidgetzones();
            var returnStr = new List<string>();
            foreach (var bp in bannerPlacements)
                returnStr.Add(bp.WidgetZone);
            return returnStr;
            //return new List<string>() { "home_page_top" };
        }

        /// <summary>
        /// Gets a route for displaying widget
        /// </summary>
        /// <param name="widgetZone">Widget zone where it's displayed</param>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetDisplayWidgetRoute(string widgetZone, out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "BannerInfo";
            controllerName = "WidgetsIkBanner";
            routeValues = new RouteValueDictionary()
            {
                {"Namespaces", "Nop.Plugin.Widgets.IkBanner.Controllers"},
                {"area", null},
                {"widgetZone", widgetZone}
            };
        }
        

        public override void Install()
        {
            //database objects
            _objectContext.Install();

            //Add banner widgetzones
            var bWidgetzone = new IkBannerWidgetzone();
            bWidgetzone.IsStatic = false;
            bWidgetzone.WidgetZone = "home_page_top";
            _bannerService.InsertBannerWidgetzone(bWidgetzone);
            bWidgetzone = new IkBannerWidgetzone();
            bWidgetzone.IsStatic = true;
            bWidgetzone.WidgetZone = "home_page_right";
            _bannerService.InsertBannerWidgetzone(bWidgetzone);
            bWidgetzone = new IkBannerWidgetzone();
            bWidgetzone.IsStatic = false;
            bWidgetzone.WidgetZone = "categorydetails_top";
            _bannerService.InsertBannerWidgetzone(bWidgetzone);

            //Add sample banner
            var bannerRecord = new IkBanner.Domain.IkBanner();
            bannerRecord.StoreId = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<p><img src=\"/Plugins/Widgets.IkBanner/images/11.png\" alt=\"\" /></p>");
            sb.AppendLine("<article>");
            sb.AppendLine("<h2>Welcome to KA - Home</h2>");
            sb.AppendLine("<h3>Hundreads of europian-inspired iteams,shipping directly to your door</h3>");
            sb.AppendLine("<a class=\"link\" href=\"#\">Details</a>");
            sb.AppendLine("</article>");
            bannerRecord.BannerHtml = sb.ToString();
            bannerRecord.WidgetzoneId = 1;
            _bannerService.InsertBanner(bannerRecord);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Store", "Select Store");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Store.Hint", "Select Store");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Html", "Banner");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Html.hint", "Banner Html");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Category", "Select Category");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Category.Hint", "Select Category");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Placement", "Select Widgetzone");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Placement.Hint", "Select Widgetzone");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.addrecord", "Add Banner");
            this.AddOrUpdatePluginLocaleResource("plugins.widgets.ikbanner.general", "General");
            this.AddOrUpdatePluginLocaleResource("plugins.widgets.ikbanner.placement", "WidgetZones");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.WidgetZone", "Select Widgetzone");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.WidgetZone.Hint", "Select Widgetzone");
            this.AddOrUpdatePluginLocaleResource("plugins.widgets.ikbanner.addwidgetpopup", "Add Widgetzone");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.IsStatic", "Disable slider?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.IsStatic.Hint", "Choose this if you don't want a slider for this widget zone. This is required sometimes when you just want to show one image or few images without a slider.");
            base.Install();
        }

        public override void Uninstall()
        {
            //database objects
            _objectContext.Uninstall();
            //locales
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Store");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Store.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Html");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.Html.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Category");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Placement");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.addrecord");
            this.DeletePluginLocaleResource("plugins.widgets.ikbanner.general");
            this.DeletePluginLocaleResource("plugins.widgets.ikbanner.placement");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.WidgetZone");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.WidgetZone.Hint");
            this.DeletePluginLocaleResource("plugins.widgets.ikbanner.addwidgetpopup");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.IsStatic");
            this.DeletePluginLocaleResource("Plugins.Widgets.IkBanner.Fields.IsStatic.Hint");
            base.Uninstall();
        }

        #endregion
    }
}
