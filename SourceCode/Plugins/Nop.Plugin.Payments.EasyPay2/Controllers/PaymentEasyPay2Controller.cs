using Nop.Core;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.EasyPay2.Models;
using Nop.Plugin.Payments.EasyPay2.Validators;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.EasyPay2.Controllers
{
    public class PaymentEasyPay2Controller : BasePaymentController
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
        private readonly EasyPay2PaymentSettings _easyPay2PaymentSettings;

        public PaymentEasyPay2Controller(IWorkContext workContext,
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
            EasyPay2PaymentSettings easyPay2PaymentSettings)
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
            this._easyPay2PaymentSettings = easyPay2PaymentSettings;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //var model = new ConfigurationModel();
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var easyPay2PaymentSettings = _settingService.LoadSetting<EasyPay2PaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            //model.UseSandbox = easyPay2PaymentSettings.UseSandbox;
            //model.BusinessEmail = easyPay2PaymentSettings.BusinessEmail;
            
            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                //model.UseSandbox_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.UseSandbox, storeScope);
            }
            Debug.WriteLine("Configure !!!");
            return View("~/Plugins/Payments.EasyPay2/Views/PaymentEasyPay2/Configure.cshtml", model);
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
            var payPalStandardPaymentSettings = _settingService.LoadSetting<EasyPay2PaymentSettings>(storeScope);
 
            //now clear settings cache
            _settingService.ClearCache();

            //SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();

            //CC types
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "VISA Credit Card",
                Value = "2",
            });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "Master Credit Card",
                Value = "3",
            });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "Amex Credit Card",
                Value = "5",
            });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "Diners Credit Card",
                Value = "22",
            });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "JCB Credit Card",
                Value = "23",
            });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "China UnionPay Card",
                Value = "25",
            });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "ENets",
                Value = "41",
            });

            //years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem
                {
                    Text = year,
                    Value = year.Substring(2),
                });
            }

            //months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i : i.ToString();
                model.ExpireMonths.Add(new SelectListItem
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            var form = this.Request.Form;
            model.CardholderName = form["CardholderName"];
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];
            var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase));
            if (selectedCcType != null) {
                selectedCcType.Selected = true;
            }
            var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
            if (selectedMonth != null) {
                selectedMonth.Selected = true;
            }
            var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
            if (selectedYear != null) {
                selectedYear.Selected = true;
            }
            return View("~/Plugins/Payments.EasyPay2/Views/PaymentEasyPay2/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                    warnings.Add(error.ErrorMessage);
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];
            paymentInfo.CreditCardNumber = form["CardNumber"];
            paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            paymentInfo.CreditCardCvv2 = form["CardCode"];
            return paymentInfo;
        }
    }
}
