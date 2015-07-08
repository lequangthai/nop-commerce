using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Widgets.IkBanner.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Plugin.Widgets.IkBanner.Services;
using System.Text;
using Nop.Core.Domain.Directory;
using Nop.Web.Framework.Mvc;
using Nop.Plugin.Widgets.IkBanner.Domain;
using Nop.Services.Catalog;

namespace Nop.Plugin.Widgets.IkBanner.Controllers
{
    public class WidgetsIkBannerController : BasePluginController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IBannerService _bannerService;
        private readonly IStoreContext _storeContext;
        private readonly ICategoryService _categoryService;

        public WidgetsIkBannerController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            ILocalizationService localizationService,
            IBannerService bannerService, IStoreContext storeContext, ICategoryService categoryService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._bannerService = bannerService;
            this._storeContext = storeContext;
            this._categoryService = categoryService;
        }

        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            //little hack here
            //always set culture to 'en-US' (Telerik has a bug related to editing decimal values in other cultures). Like currently it's done for admin area in Global.asax.cs
            CommonHelper.SetTelerikCulture();

            base.Initialize(requestContext);
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new BannerInfoModel();            
            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/Configure.cshtml", model);
        }

   

        [HttpPost]
        public ActionResult BannerList(DataSourceRequest command)
        {
            var records = _bannerService.GetAll(command.Page - 1, command.PageSize);
            var sbwModel = records.Select(x =>
            {
                var m = new BannerInfoModel()
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    CategoryId = x.CategoryId,
                    WidgetzoneId = x.WidgetzoneId,
                    BannerHtml = x.BannerHtml,
                };
                //store
                var store = _storeService.GetStoreById(x.StoreId);
                m.StoreName = (store != null) ? store.Name : "*";
                var category = _categoryService.GetCategoryById(x.CategoryId);
                m.CategoryName = (category != null) ? category.Name : "*";
                var WidgetZones = _bannerService.GetBannerWidgetzoneById(x.WidgetzoneId);
                m.WidgetZone = (WidgetZones != null) ? WidgetZones.WidgetZone : "";
                return m;
            })
                .ToList();
            var gridModel = new DataSourceResult
            {
                Data = sbwModel,
                Total = records.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public ActionResult BannerDelete(int id)
        {

            var sbw = _bannerService.GetById(id);
            if (sbw != null)
                _bannerService.DeleteBanner(sbw);

            return new NullJsonResult();
        }

        [HttpPost]
        public ActionResult BannerWidgetzoneList(DataSourceRequest command)
        {
            var records = _bannerService.GetAllBannerWidgetzones(command.Page - 1, command.PageSize);
            var sbwModel = records.Select(x =>
            {
                var m = new BannerWidgetzoneModel()
                {
                    Id = x.Id,
                    Widgetzone = x.WidgetZone,
                    IsStatic = x.IsStatic,
                };
                return m;
            })
                .ToList();
            var gridModel = new DataSourceResult
            {
                Data = sbwModel,
                Total = records.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public ActionResult BannerWidgetzoneDelete(int id)
        {

            var sbw = _bannerService.GetBannerWidgetzoneById(id);
            if (sbw != null)
                _bannerService.DeleteBannerWidgetzone(sbw);

            return new NullJsonResult();
        }

        [ChildActionOnly]
        public ActionResult BannerInfo(string widgetZone, int? categoryId)
        {
            var model = new BannerModel();
            var bannerWidgetzone = _bannerService.GetBannerWidgetzoneByZone(widgetZone);
            if (bannerWidgetzone == null)
                return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/BannerInfo.cshtml", model);
            model.IsStatic = bannerWidgetzone.IsStatic;
            var banners = _bannerService.GetAllByWidgetzone(bannerWidgetzone.Id, categoryId.HasValue ? categoryId.Value : 0);
            foreach(var banner in banners)
            {
                if (banner.StoreId == 0 || banner.StoreId == _storeContext.CurrentStore.Id)
                {
                    var bim = new BannerInfoModel();
                    bim.Id = banner.Id;
                    bim.BannerHtml = banner.BannerHtml;
                    bim.StoreId = banner.StoreId;
                    bim.CategoryId = banner.CategoryId;
                    bim.WidgetzoneId = banner.WidgetzoneId;
                    model.AvailableBanners.Add(bim);
                }
            }
            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/BannerInfo.cshtml", model);
        }

        //edit
        public ActionResult EditPopup(int id)
        {
            var sbw = _bannerService.GetById(id);
            if (sbw == null)
                //No record found with the specified id
                return RedirectToAction("Configure");

            var model = new BannerInfoModel()
            {
                Id = sbw.Id,
                StoreId = sbw.StoreId,
                BannerHtml = sbw.BannerHtml,
                CategoryId = sbw.CategoryId,
                WidgetzoneId = sbw.WidgetzoneId
            };          

            var selectedStore = _storeService.GetStoreById(sbw.StoreId);            
            //stores
            model.AvailableStores.Add(new SelectListItem() { Text = "*", Value = "0" });
            foreach (var store in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem() { Text = store.Name, Value = store.Id.ToString(), Selected = (selectedStore != null && store.Id == selectedStore.Id) });

            var selectedCategory = _categoryService.GetCategoryById(sbw.CategoryId);
            model.AvailableCategories.Add(new SelectListItem() { Text = "*", Value = "0" });
            foreach (var category in _categoryService.GetAllCategories())
                model.AvailableCategories.Add(new SelectListItem() { Text = category.Name, Value = category.Id.ToString(), Selected = (selectedCategory != null && category.Id == selectedCategory.Id) });

            var selectedPlacement = _bannerService.GetBannerWidgetzoneById(sbw.WidgetzoneId);
            foreach (var placement in _bannerService.GetAllBannerWidgetzones())
                model.AvailableWidgetZones.Add(new SelectListItem() { Text = placement.WidgetZone, Value = placement.Id.ToString(), Selected = (selectedCategory != null && placement.Id == selectedPlacement.Id) });

            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/EditPopup.cshtml", model);
        }
        [HttpPost]
        public ActionResult EditPopup(string btnId, string formId, BannerInfoModel model)
        {
            var sbw = _bannerService.GetById(model.Id);
            if (sbw == null)
                //No record found with the specified id
                return RedirectToAction("Configure");

            sbw.StoreId = model.StoreId;
            sbw.BannerHtml = model.BannerHtml;
            sbw.CategoryId = model.CategoryId;
            sbw.WidgetzoneId = model.WidgetzoneId;
            _bannerService.UpdateBanner(sbw);

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/EditPopup.cshtml", model);
        }

        //add
        public ActionResult AddPopup()
        {
            var model = new BannerInfoModel();
            model.AvailableStores.Add(new SelectListItem() { Text = "*", Value = "0" });
            foreach (var store in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem() { Text = store.Name, Value = store.Id.ToString() });

            model.AvailableCategories.Add(new SelectListItem() { Text = "*", Value = "0" });
            foreach (var category in _categoryService.GetAllCategories())
                model.AvailableCategories.Add(new SelectListItem() { Text = category.Name, Value = category.Id.ToString() });

            foreach (var placement in _bannerService.GetAllBannerWidgetzones())
                model.AvailableWidgetZones.Add(new SelectListItem() { Text = placement.WidgetZone, Value = placement.Id.ToString() });

            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/AddPopup.cshtml", model);
        }
        [HttpPost]
        public ActionResult AddPopup(string btnId, string formId, BannerInfoModel model)
        {
            var bannerRecord = new IkBanner.Domain.IkBanner();
            bannerRecord.BannerHtml = model.BannerHtml;
            bannerRecord.StoreId = model.StoreId;
            bannerRecord.CategoryId = model.CategoryId;
            bannerRecord.WidgetzoneId = model.WidgetzoneId;
            _bannerService.InsertBanner(bannerRecord);

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/AddPopup.cshtml", model);
        }


        //add
        public ActionResult AddWidgetPopup()
        {
            var model = new BannerWidgetzoneModel();
            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/AddWidgetPopup.cshtml", model);
        }
        [HttpPost]
        public ActionResult AddWidgetPopup(string btnId, string formId, BannerWidgetzoneModel model)
        {
            var bannerRecord = new IkBannerWidgetzone();
            bannerRecord.WidgetZone = model.Widgetzone;
            bannerRecord.IsStatic = model.IsStatic;
            _bannerService.InsertBannerWidgetzone(bannerRecord);

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/AddWidgetPopup.cshtml", model);
        }

        //edit
        public ActionResult EditWidgetPopup(int id)
        {
            var sbw = _bannerService.GetBannerWidgetzoneById(id);
            if (sbw == null)
                //No record found with the specified id
                return RedirectToAction("Configure");

            var model = new BannerWidgetzoneModel()
            {
                Id = sbw.Id,
                Widgetzone = sbw.WidgetZone,
                IsStatic = sbw.IsStatic,
            };

            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/EditWidgetPopup.cshtml", model);
        }
        [HttpPost]
        public ActionResult EditWidgetPopup(string btnId, string formId, BannerWidgetzoneModel model)
        {
            var sbw = _bannerService.GetBannerWidgetzoneById(model.Id);
            if (sbw == null)
                //No record found with the specified id
                return RedirectToAction("Configure");

            sbw.WidgetZone = model.Widgetzone;
            sbw.IsStatic = model.IsStatic;
            _bannerService.UpdateBannerWidgetzone(sbw);

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View("~/Plugins/Widgets.IkBanner/Views/WidgetsBanner/EditWidgetPopup.cshtml", model);
        }

    }
}