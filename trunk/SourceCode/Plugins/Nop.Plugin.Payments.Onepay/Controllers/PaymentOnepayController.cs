using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Framework.Controllers;
using Nop.Services.Configuration;
using System.Web.Mvc;
using System.Diagnostics;
using Nop.Plugin.Payments.Onepay.Models;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Payments;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Tax;
using Nop.Core;
using Nop.Core.Domain.Directory;
using System.Web;
using Nop.Services.Stores;
using Nop.Services.Logging;

namespace Nop.Plugin.Payments.Onepay.Controllers
{
    public class PaymentOnepayController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;
        private readonly OnepayPaymentSettings _onepayPaymentSettings;

        public PaymentOnepayController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IPaymentService paymentService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            ILogger logger, 
            IWebHelper webHelper,
            PaymentSettings paymentSettings,
            OnepayPaymentSettings payPalStandardPaymentSettings)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._localizationService = localizationService;
            this._storeContext = storeContext;
            this._logger = logger;
            this._webHelper = webHelper;
            this._paymentSettings = paymentSettings;
            this._onepayPaymentSettings = payPalStandardPaymentSettings;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //var model = new ConfigurationModel();
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalStandardPaymentSettings = _settingService.LoadSetting<OnepayPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.UseSandbox = payPalStandardPaymentSettings.UseSandbox;
            model.BusinessEmail = payPalStandardPaymentSettings.BusinessEmail;
            model.PdtToken = payPalStandardPaymentSettings.PdtToken;
            model.PdtValidateOrderTotal = payPalStandardPaymentSettings.PdtValidateOrderTotal;
            model.AdditionalFee = payPalStandardPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = payPalStandardPaymentSettings.AdditionalFeePercentage;
            model.PassProductNamesAndTotals = payPalStandardPaymentSettings.PassProductNamesAndTotals;
            model.EnableIpn = payPalStandardPaymentSettings.EnableIpn;
            model.IpnUrl = payPalStandardPaymentSettings.IpnUrl;
            model.AddressOverride = payPalStandardPaymentSettings.AddressOverride;
            model.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage = payPalStandardPaymentSettings.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.UseSandbox, storeScope);
                model.BusinessEmail_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.BusinessEmail, storeScope);
                model.PdtToken_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.PdtToken, storeScope);
                model.PdtValidateOrderTotal_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.PdtValidateOrderTotal, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.PassProductNamesAndTotals_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);
                model.EnableIpn_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.EnableIpn, storeScope);
                model.IpnUrl_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.IpnUrl, storeScope);
                model.AddressOverride_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.AddressOverride, storeScope);
                model.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage, storeScope);
            }
            Debug.WriteLine("Configure !!!");
            return View("~/Plugins/Payments.Onepay/Views/PaymentOnepay/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalStandardPaymentSettings = _settingService.LoadSetting<OnepayPaymentSettings>(storeScope);

            //save settings
            payPalStandardPaymentSettings.UseSandbox = model.UseSandbox;
            payPalStandardPaymentSettings.BusinessEmail = model.BusinessEmail;
            payPalStandardPaymentSettings.PdtToken = model.PdtToken;
            payPalStandardPaymentSettings.PdtValidateOrderTotal = model.PdtValidateOrderTotal;
            payPalStandardPaymentSettings.AdditionalFee = model.AdditionalFee;
            payPalStandardPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            payPalStandardPaymentSettings.PassProductNamesAndTotals = model.PassProductNamesAndTotals;
            payPalStandardPaymentSettings.EnableIpn = model.EnableIpn;
            payPalStandardPaymentSettings.IpnUrl = model.IpnUrl;
            payPalStandardPaymentSettings.AddressOverride = model.AddressOverride;
            payPalStandardPaymentSettings.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage = model.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.UseSandbox_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.UseSandbox, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.UseSandbox, storeScope);

            if (model.BusinessEmail_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.BusinessEmail, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.BusinessEmail, storeScope);

            if (model.PdtToken_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.PdtToken, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.PdtToken, storeScope);

            if (model.PdtValidateOrderTotal_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.PdtValidateOrderTotal, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.PdtValidateOrderTotal, storeScope);

            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            if (model.PassProductNamesAndTotals_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.PassProductNamesAndTotals, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);

            if (model.EnableIpn_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.EnableIpn, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.EnableIpn, storeScope);

            if (model.IpnUrl_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.IpnUrl, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.IpnUrl, storeScope);

            if (model.AddressOverride_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.AddressOverride, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.AddressOverride, storeScope);

            if (model.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(payPalStandardPaymentSettings, x => x.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(payPalStandardPaymentSettings, x => x.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.Onepay/Views/PaymentOnepay/PaymentInfo.cshtml");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }
      
    }
}
