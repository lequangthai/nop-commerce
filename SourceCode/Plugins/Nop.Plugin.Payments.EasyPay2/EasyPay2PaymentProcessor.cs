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
using System.Collections.Specialized;
using System.Net;
using Nop.Services.Logging;

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
        private readonly ILogger _logger;

        public EasyPay2PaymentProcessor(EasyPay2PaymentSettings easyPay2PaymentSettings,
            ISettingService settingService, ICurrencyService currencyService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            ICheckoutAttributeParser checkoutAttributeParser, ITaxService taxService,
            IOrderTotalCalculationService orderTotalCalculationService, HttpContextBase httpContext, IEncryptionService encryptionService, ILogger logger)
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
            this._logger = logger;
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
            var webClient = new WebClient();
            var form = new NameValueCollection();
            var order = capturePaymentRequest.Order;
            var result = new CapturePaymentResult();
            form.Add("mid", _easyPay2PaymentSettings.mid);
            form.Add("ref", order.OrderGuid.ToString());
            form.Add("cur", _easyPay2PaymentSettings.cur);
            form.Add("amt", order.OrderTotal.ToString());
            form.Add("paytype", "3");
            form.Add("transtype", "capture");
            var responseData = webClient.UploadValues(GetPaymentProcessUrl(), form);
            var reply = Encoding.ASCII.GetString(responseData);
            string[] responseFields = reply.Split('&');
            List<String> errorList = new List<String>();
            if (("YES").Equals(responseFields[4].Split('=')[1]))
            {
                _logger.Information("Void action success !!!");
                result.NewPaymentStatus = PaymentStatus.Paid;
            }
            else
            {
                errorList.Add(responseFields[5].Split('=')[1]);
                errorList.Add(responseFields[11].Split('=')[1]);
                result.Errors = errorList;
                _logger.Error("OOPS void action !!!" + responseFields[5].Split('=')[1] + " " + responseFields[11].Split('=')[1]);
            }
            return result;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart, IList<ShippingCart> shippingCarts)
        {
            Debug.WriteLine("GetAdditionalHandlingFee");
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart, shippingCarts,
                _easyPay2PaymentSettings.additionalFee, _easyPay2PaymentSettings.additionalFeePercentage);
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
            Debug.WriteLine("PostProcessPayment");

            var nfi = new CultureInfo("en-US", false).NumberFormat;
            var url = GetPaymentUrl();
            var gatewayUrl = new Uri(url);
            var post = new RemotePost { Url = gatewayUrl.ToString(), Method = "POST" };
            var order = postProcessPaymentRequest.Order;

            post.Add("mid", _easyPay2PaymentSettings.mid);
            post.Add("ref", order.OrderGuid.ToString());
            post.Add("cur", _easyPay2PaymentSettings.cur);
            post.Add("amt", order.OrderTotal.ToString());
            post.Add("ccnum", _encryptionService.DecryptText(order.CardNumber));
            post.Add("ccdate", _encryptionService.DecryptText(order.CardExpirationYear) + _encryptionService.DecryptText(order.CardExpirationMonth));
            post.Add("cccvv", _encryptionService.DecryptText(order.CardCvv2));
            post.Add("paytype", "3");

            if (_easyPay2PaymentSettings.transactMode == TransactMode.Authorize)
                post.Add("transtype", "auth");
            else if (_easyPay2PaymentSettings.transactMode == TransactMode.AuthorizeAndCapture)
                post.Add("transtype", "sale");
            else
                throw new NopException("Not supported transaction mode");

            post.Add("statusurl", GetReturnStatusUrl("statusurl", postProcessPaymentRequest));
            post.Add("returnurl", GetReturnStatusUrl("returnurl", postProcessPaymentRequest));
            Debug.Print("ccdate: " + _encryptionService.DecryptText(order.CardExpirationYear) + _encryptionService.DecryptText(order.CardExpirationMonth));
            post.Post();
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
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
                return true;
            }
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var webClient = new WebClient();
            var form = new NameValueCollection();
            var order = voidPaymentRequest.Order;
            var result = new VoidPaymentResult();
            form.Add("mid", _easyPay2PaymentSettings.mid);
            form.Add("ref", order.OrderGuid.ToString());
            form.Add("cur", _easyPay2PaymentSettings.cur);
            form.Add("amt", order.OrderTotal.ToString());
            form.Add("paytype", "3");
            form.Add("transtype", "void");
            form.Add("subtranstype", "auth");
            var responseData = webClient.UploadValues(GetPaymentProcessUrl(), form);
            var reply = Encoding.ASCII.GetString(responseData);
            string[] responseFields = reply.Split('&');
            List<String> errorList = new List<String>();
            if (("YES").Equals(responseFields[4].Split('=')[1]))
            {
                _logger.Information("Void action success !!!");
                result.NewPaymentStatus = PaymentStatus.Voided;
            }
            else
            {
                errorList.Add(responseFields[5].Split('=')[1]);
                errorList.Add(responseFields[11].Split('=')[1]);
                result.Errors = errorList;
                _logger.Error("OOPS void action !!!" + responseFields[5].Split('=')[1] + " " + responseFields[11].Split('=')[1]);
            }
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
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.RedirectionTip", "You will be redirected to EasyPay site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.mid", "Merchant ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.cur", "Currentcy");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.transtype", "transtyle");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.TransactModeValues", "Transaction mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.TransactModeValues.Hint", "Choose transaction mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.url", "URL payment gateway");
            
            base.Install();
        }

        public override void Uninstall()
        {
            Debug.WriteLine("Uninstall");
            //settings
            _settingService.DeleteSetting<EasyPay2PaymentSettings>();
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.mid");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.cur");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.transtype");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.TransactModeValues");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.TransactModeValues.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.EasyPay2.Fields.url");
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
                return true;
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
            return _easyPay2PaymentSettings.url;
        }

        /// <summary>
        /// Gets payment URL
        /// </summary>
        /// <returns></returns>
        private string GetPaymentProcessUrl()
        {
            return "https://test.wirecard.com.sg/easypay2/paymentprocess.do";
        }

        /// <summary>
        /// Gets payment URL
        /// </summary>
        /// <returns></returns>
        private string GetReturnStatusUrl(string type, PostProcessPaymentRequest postProcessPaymentRequest)
        {
            string leftPath = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
            var order = postProcessPaymentRequest.Order;
            StringBuilder url = new StringBuilder();
            url.Append(leftPath);
            url.Append("/Plugins/PaymentEasyPay2/statusHandler");
            url.Append("?orderId=" + order.Id);
            url.Append("&orderGuid=" + order.OrderGuid);
            if (type.Equals("statusurl"))
	        {
		        url.Append("&type=statusUrl");
	        } else {
                url.Append("&type=returnUrl");
            }

            return url.ToString();
        }

        #endregion
    }
}
