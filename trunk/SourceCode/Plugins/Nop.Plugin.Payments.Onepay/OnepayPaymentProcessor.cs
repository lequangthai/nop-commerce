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
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Onepay.Controllers;
using System.Globalization;
using Nop.Web.Framework;
using System.Security.Cryptography;
using Nop.Plugin.Payments.Onepay;


namespace Nop.Plugin.Payments.Onepay
{
    public class OnepayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly OnepayPaymentSettings _onePayPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ITaxService _taxService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly HttpContextBase _httpContext;

       // SortedList<String, String> _requestFields = new SortedList<String, String>(new VPCStringComparer());
        String _rawResponse;
       // SortedList<String, String> _responseFields = new SortedList<String, String>(new VPCStringComparer());
       //String _secureSecret = "A3EFDFABA8653DF2342E8DAC29B51AF0";


        public OnepayPaymentProcessor(OnepayPaymentSettings onepayPaymentSettings,
            ISettingService settingService, ICurrencyService currencyService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            ICheckoutAttributeParser checkoutAttributeParser, ITaxService taxService, 
            IOrderTotalCalculationService orderTotalCalculationService, HttpContextBase httpContext)
        {
            this._onePayPaymentSettings = onepayPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._taxService = taxService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._httpContext = httpContext;
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

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart, IList<ShippingCart> shippingCarts)
        {
            Debug.WriteLine("GetAdditionalHandlingFee");
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart, shippingCarts,
                _onePayPaymentSettings.AdditionalFee, _onePayPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            Debug.WriteLine("GetConfigurationRoute");
            actionName = "Configure";
            controllerName = "PaymentOnepay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.Onepay.Controllers" }, { "area", null } };
            //throw new NotImplementedException();
        }

        public Type GetControllerType()
        {
            Debug.WriteLine("GetControllerType");
            return typeof(PaymentOnepayController);
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentOnepay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.Onepay.Controllers" }, { "area", null } };
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
            post.Add("ccnum", "4111111111111111");

            post.Add("ccdate", "1511");
            post.Add("cccvv", "989");
            post.Add("paytype", "3");
            post.Add("transtype", "sale");

            post.Add("returnurl", "http://localhost:15536/onepagecheckout");
            
            post.Post();
        }

        //public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        //{
        //    RandomStringGenerator RSG = new RandomStringGenerator();
        //    RSG.MinLowerCaseCharacters = 25;
        //    RSG.RepeatCharacters = false;
            
        //    Debug.WriteLine(RSG.Generate(25));
        //    Debug.WriteLine("PostProcessPayment");
            
        //    var nfi = new CultureInfo("en-US", false).NumberFormat;
        //    var url = GetPaymentUrl();
        //    var gatewayUrl = new Uri(url);
        //    var post = new RemotePost { Url = gatewayUrl.ToString(), Method = "POST" };
        //    var order = postProcessPaymentRequest.Order;
        //    var merchTxnRef = RSG.Generate(25);
        //    var orderInfo = RSG.Generate(25);

        //    post.Add("Title", "onepay paygate");
        //    post.Add("vpc_Locale", "vn");
        //    post.Add("vpc_Version", "2");
        //    post.Add("vpc_Command", "pay");
        //    post.Add("vpc_Merchant", "ONEPAY");
        //    post.Add("vpc_AccessCode", "D67342C2");
        //    post.Add("vpc_MerchTxnRef", merchTxnRef);
        //    post.Add("vpc_OrderInfo", "TEST" + orderInfo);
        //    post.Add("vpc_Amount", "200000");
        //    post.Add("vpc_Currency", "VND");
        //    post.Add("vpc_ReturnURL", "http://localhost:15536/onepagecheckout");

        //    post.Add("vpc_SHIP_Street01", "");
        //    post.Add("vpc_SHIP_Provice", "");
        //    post.Add("vpc_SHIP_City", "");
        //    post.Add("vpc_SHIP_Country", "");
        //    post.Add("vpc_Customer_Phone", "");
        //    post.Add("vpc_Customer_Email", "");
        //    post.Add("vpc_Customer_Id", "");

        //    post.Add("vpc_TicketNo", order.CustomerIp);

            
            
        //    //-------------------
        //    string SECURE_SECRET = "A3EFDFABA8653DF2342E8DAC29B51AF0"; 
        //    VPCRequest conn = new VPCRequest("https://mtf.onepay.vn/onecomm-pay/vpc.op");
        //    conn.SetSecureSecret(SECURE_SECRET);
        //    // Add the Digital Order Fields for the functionality you wish to use
        //    // Core Transaction Fields
        //    conn.AddDigitalOrderField("Title", "onepay paygate");
        //    conn.AddDigitalOrderField("vpc_Locale", "vn");//Chon ngon ngu hien thi tren cong thanh toan (vn/en)
        //    conn.AddDigitalOrderField("vpc_Version", "2");
        //    conn.AddDigitalOrderField("vpc_Command", "pay");
        //    conn.AddDigitalOrderField("vpc_Merchant", "ONEPAY");
        //    conn.AddDigitalOrderField("vpc_AccessCode", "D67342C2");
        //    conn.AddDigitalOrderField("vpc_MerchTxnRef", merchTxnRef);
        //    conn.AddDigitalOrderField("vpc_OrderInfo", "TEST" + orderInfo);
        //    conn.AddDigitalOrderField("vpc_Amount", "200000");
        //    conn.AddDigitalOrderField("vpc_Currency", "VND");
        //    conn.AddDigitalOrderField("vpc_ReturnURL", "http://localhost:15536/onepagecheckout");
        //    // Thong tin them ve khach hang. De trong neu khong co thong tin
        //    conn.AddDigitalOrderField("vpc_SHIP_Street01", "");
        //    conn.AddDigitalOrderField("vpc_SHIP_Provice", "");
        //    conn.AddDigitalOrderField("vpc_SHIP_City", "");
        //    conn.AddDigitalOrderField("vpc_SHIP_Country", "");
        //    conn.AddDigitalOrderField("vpc_Customer_Phone", "");
        //    conn.AddDigitalOrderField("vpc_Customer_Email", "");
        //    conn.AddDigitalOrderField("vpc_Customer_Id", "");
        //    // Dia chi IP cua khach hang
        //    conn.AddDigitalOrderField("vpc_TicketNo", order.CustomerIp);

        //    post.Add("vpc_SecureHash", conn.CreateSHA256Signature(true));
        //    post.Post();
        //}

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            Debug.WriteLine("PostProcessPayment");
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
            //throw new NotImplementedException();
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
            Debug.WriteLine("Installlllllllllllllllll");
            //settings
            var settings = new OnepayPaymentSettings
            {
                UseSandbox = true,
                BusinessEmail = "test@test.com",
                PdtToken = "Your PDT token here...",
                PdtValidateOrderTotal = true,
                EnableIpn = true,
                AddressOverride = true,
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.RedirectionTip", "You will be redirected to Onepay site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.BusinessEmail", "Business Email");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.BusinessEmail.Hint", "Specify your Onepay business email.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.PDTToken", "PDT Identity Token");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.PDTToken.Hint", "Specify PDT identity token");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.PDTValidateOrderTotal", "PDT. Validate order total");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.PDTValidateOrderTotal.Hint", "Check if PDT handler should validate order totals.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.PassProductNamesAndTotals", "Pass product names and order totals to PayPal");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.PassProductNamesAndTotals.Hint", "Check if product names and order totals should be passed to PayPal.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.EnableIpn", "Enable IPN (Instant Payment Notification)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.EnableIpn.Hint", "Check if IPN is enabled.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.EnableIpn.Hint2", "Leave blank to use the default IPN handler URL.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.IpnUrl", "IPN Handler");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.IpnUrl.Hint", "Specify IPN Handler.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.AddressOverride", "Address override");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.AddressOverride.Hint", "For people who already have PayPal accounts and whom you already prompted for a shipping address before they choose to pay with PayPal, you can use the entered address instead of the address the person has stored with PayPal.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage", "Return to order details page");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Onepay.Fields.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage.Hint", "Enable if a customer should be redirected to the order details page when he clicks \"return to store\" link on PayPal site WITHOUT completing a payment");

            base.Install();
        }

        public void Uninstall()
        {
            Debug.WriteLine("Uninstallllllllllll");
            //settings
            _settingService.DeleteSetting<OnepayPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.BusinessEmail");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.BusinessEmail.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.PDTToken");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.PDTToken.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.PDTValidateOrderTotal");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.PDTValidateOrderTotal.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.PassProductNamesAndTotals");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.PassProductNamesAndTotals.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.EnableIpn");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.EnableIpn.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.EnableIpn.Hint2");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.IpnUrl");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.IpnUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.AddressOverride");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.AddressOverride.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage");
            this.DeletePluginLocaleResource("Plugins.Payments.Onepay.Fields.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage.Hint");

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
            //return _onePayPaymentSettings.UseSandbox ? "https://mtf.onepay.vn/onecomm-pay/vpc.op" :
            //    "https://mtf.onepay.vn/onecomm-pay/vpc.op";

            return _onePayPaymentSettings.UseSandbox ? "https://test.wirecard.com.sg/easypay2/paymentpage.do" :
                "https://test.wirecard.com.sg/easypay2/paymentpage.do";
        }

        /// <summary>
        /// Gets verify URL
        /// </summary>
        /// <returns></returns>
        //private string GetVerifyUrl()
        //{
        //    return _monerisPaymentSettings.UseSandbox ? "https://esqa.moneris.com/HPPDP/verifyTxn.php" :
        //        "https://www3.moneris.com/HPPDP/verifyTxn.php";
        //}

        /// <summary>
        /// Transaction verification
        /// </summary>
        /// <param name="transactionKey">transactionKey</param>
        /// <param name="values">values</param>
        /// <returns>Result</returns>


        #endregion
    }
}
