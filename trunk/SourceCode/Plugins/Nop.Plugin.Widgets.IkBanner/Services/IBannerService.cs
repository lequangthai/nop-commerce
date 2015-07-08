using System.Collections.Generic;
using Nop.Plugin.Widgets.IkBanner.Domain;

namespace Nop.Plugin.Widgets.IkBanner.Services
{
    public partial interface IBannerService
    {
        void DeleteBanner(IkBanner.Domain.IkBanner bannerRecord);

        IList<IkBanner.Domain.IkBanner> GetAll(int pageIndex = 0, int pageSize = int.MaxValue);

        IList<IkBanner.Domain.IkBanner> GetAllByWidgetzone(int WidgetzoneId, int categoryId);

        //IList<string> GetAllBannerPlacements();

        IkBanner.Domain.IkBanner GetById(int Id);

        void InsertBanner(IkBanner.Domain.IkBanner bannerRecord);

        void UpdateBanner(IkBanner.Domain.IkBanner bannerRecord);

        #region BannerPlacements

        IList<IkBannerWidgetzone> GetAllBannerWidgetzones(int pageIndex = 0, int pageSize = int.MaxValue);

        IkBannerWidgetzone GetBannerWidgetzoneById(int WidgetzoneId);

        IkBannerWidgetzone GetBannerWidgetzoneByZone(string widgetZone);

        void DeleteBannerWidgetzone(IkBannerWidgetzone widgetZone);

        void InsertBannerWidgetzone(IkBannerWidgetzone widgetZone);

        void UpdateBannerWidgetzone(IkBannerWidgetzone widgetZone);

        #endregion

    }
}
