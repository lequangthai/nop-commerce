using Nop.Core;
using Nop.Core.Domain.Directory;

using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.EasyPay2.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using Nop.Web.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using Nop.Services.Localization;
using Nop.Core.Domain.Orders;
using Nop.Services.Security;

namespace Nop.Plugin.Payments.EasyPay2
{
    public class EasyPay2PaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly EasyPay2PaymentSettings _easyPay2PaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ITaxService _taxService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly HttpContextBase _httpContext;
        private readonly IEncryptionService _encryptionService;

        public EasyPay2PaymentProcessor(EasyPay2PaymentSettings easyPay2PaymentSettings,
            ISettingService settingService, ICurrencyService currencyService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            ICheckoutAttributeParser checkoutAttributeParser, ITaxService taxService,
            IOrderTotalCalculationService orderTotalCalculationService, HttpContextBase httpContext, IEncryptionService encryptionService)
        {
            this._easyPay2PaymentSettings = easyPay2PaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._taxService = taxService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._httpContext = httpContext;
            this._encryptionService = encryptionService;
        }

        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");
            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;
            return true;
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            Debug.WriteLine("GetAdditionalHandlingFee");
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _easyPay2PaymentSettings.AdditionalFee, _easyPay2PaymentSettings.AdditionalFeePercentage);
            return result;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            Debug.WriteLine("GetConfigurationRoute");
            actionName = "Configure";
            controllerName = "PaymentEasyPay2";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.EasyPay2.Controllers" }, { "area", null } };
            //throw new NotImplementedException();
        }

        public Type GetControllerType()
        {
            Debug.WriteLine("GetControllerType");
            return typeof(PaymentEasyPay2Controller);
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentEasyPay2";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.EasyPay2.Controllers" }, { "area", null } };
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            Debug.WriteLine("HidePaymentMethod");
            return false;
            //throw new NotImplementedException();
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            RandomStringGenerator RSG = new RandomStringGenerator();
            RSG.MinLowerCaseCharacters = 25;
            RSG.RepeatCharacters = false;
            var orderPaymentInfo = _httpContext.Session["OrderPaymentInfo"];//_httpContext.Session["OrderPaymentInfo"];
            Debug.WriteLine(RSG.Generate(25));
            Debug.WriteLine("PostProcessPayment");

            var nfi = new CultureInfo("en-US", false).NumberFormat;
            var url = GetPaymentUrl();
            var gatewayUrl = new Uri(url);
            var post = new RemotePost { Url = gatewayUrl.ToString(), Method = "POST" };
            var order = postProcessPaymentRequest.Order;
            var merchTxnRef = RSG.Generate(25);
            var orderInfo = RSG.Generate(25);

            post.Add("mid", "20130114001");
            post.Add("ref", merchTxnRef);
            post.Add("cur", "SGD");
            post.Add("amt", "20");
            post.Add("ccnum", _encryptionService.DecryptText(order.CardNumber));
            post.Add("ccdate", _encryptionService.DecryptText(order.CardExpirationYear) + _encryptionService.DecryptText(order.CardExpirationMonth));
            post.Add("cccvv", _encryptionService.DecryptText(order.CardCvv2));
            post.Add("paytype", "3");
            post.Add("transtype", "sale");
            post.Add("returnurl", "http://localhost:15536/onepagecheckout");
            Debug.Print("ccdate: " + _encryptionService.DecryptText(order.CardExpirationYear) + _encryptionService.DecryptText(order.CardExpirationMonth));
           
            post.Post();
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();

            result.AllowStoringCreditCardNumber = true;
            switch (_easyPay2PaymentSettings.TransactMode)
            {
                case TransactMode.Pending:
                    result.NewPaymentStatus = PaymentStatus.Pending;
                    break;
                case TransactMode.Authorize:
                    result.NewPaymentStatus = PaymentStatus.Authorized;
                    break;
                case TransactMode.AuthorizeAndCapture:
                    result.NewPaymentStatus = PaymentStatus.Paid;
                    break;
                default:
                    {
                        result.AddError("Not supported transaction type");
                        return result;
                    }
            }

            return result;
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            Debug.WriteLine("ProcessRecurringPayment");
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
            //throw new NotImplementedException();
        }



        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        public override void Install()
        {
            Debug.WriteLine("Install");
            //settings
            var settings = new EasyPay2PaymentSettings
            {
                
            };
            _settingService.SaveSetting(settings);
            //this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.RedirectionTip", "You will be redirected to Onepay site to complete the order.");
            base.Install();
        }

        public override void Uninstall()
        {
            Debug.WriteLine("Uninstall");
            //settings
            _settingService.DeleteSetting<EasyPay2PaymentSettings>();
            base.Uninstall();
        }

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }



        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Utilites

        /// <summary>
        /// Gets payment URL
        /// </summary>
        /// <returns></returns>
        private string GetPaymentUrl()
        {
            return "https://test.wirecard.com.sg/easypay2/paymentpage.do";
        }

        #endregion
    }
}
