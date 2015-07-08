using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Widgets.IkBanner.Models
{
    public class BannerInfoModel : BaseNopModel
    {
        public BannerInfoModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableCategories = new List<SelectListItem>();
            AvailableWidgetZones = new List<SelectListItem>();
        }
        public IList<SelectListItem> AvailableStores { get; set; }
        public IList<SelectListItem> AvailableCategories { get; set;}
        public IList<SelectListItem> AvailableWidgetZones { get; set; }
        public int Id { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.Store")]
        public int StoreId { get; set; }
        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.Store")]
        public string StoreName { get; set; }
        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.Html")]
        [AllowHtml]
        public string BannerHtml { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.Category")]
        public int CategoryId { get; set; }
        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.Category")]
        public string CategoryName { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.WidgetZone")]
        public int WidgetzoneId { get; set; }
        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.WidgetZone")]
        public string WidgetZone { get; set; } 

        
    }
}