using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Widgets.IkBanner.Models
{
    public class BannerWidgetzoneModel : BaseNopModel
    {
        public int Id { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.Widgetzone")]
        public string Widgetzone { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.IkBanner.Fields.IsStatic")]
        public bool IsStatic { get; set; } 
        
    }
}