using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Widgets.IkBanner.Models
{
    public class BannerModel : BaseNopModel
    {
        public BannerModel()
        {
            AvailableBanners = new List<BannerInfoModel>();
        }
        public IList<BannerInfoModel> AvailableBanners { get; set; }        

        public bool IsStatic { get; set; } 

        
    }
}