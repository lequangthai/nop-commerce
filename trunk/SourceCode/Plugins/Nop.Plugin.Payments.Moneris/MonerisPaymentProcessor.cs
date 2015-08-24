﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Xml;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.Moneris.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.Moneris
{
    public class MonerisPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly MonerisPaymentSettings _monerisPaymentSettings;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;

        #endregion

        #region Ctor

        public MonerisPaymentProcessor(ISettingService settingService, 
            MonerisPaymentSettings monerisPaymentSettings, 
            IOrderTotalCalculationService orderTotalCalculationService)
        {
            this._settingService = settingService;
            this._monerisPaymentSettings = monerisPaymentSettings;
            this._orderTotalCalculationService = orderTotalCalculationService;
        }

        #endregion

        #region Utilites

        /// <summary>
        /// Gets payment URL
        /// </summary>
        /// <returns></returns>
        private string GetPaymentUrl()
        {
            return _monerisPaymentSettings.UseSandbox ? "https://esqa.moneris.com/HPPDP/index.php" :
                "https://www3.moneris.com/HPPDP/index.php";
        }

        /// <summary>
        /// Gets verify URL
        /// </summary>
        /// <returns></returns>
        private string GetVerifyUrl()
        {
            return _monerisPaymentSettings.UseSandbox ? "https://esqa.moneris.com/HPPDP/verifyTxn.php" :
                "https://www3.moneris.com/HPPDP/verifyTxn.php";
        }

        /// <summary>
        /// Transaction verification
        /// </summary>
        /// <param name="transactionKey">transactionKey</param>
        /// <param name="values">values</param>
        /// <returns>Result</returns>
        public bool TransactionVerification(string transactionKey, out Dictionary<string, string> values)
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var url = GetVerifyUrl();
            var gatewayUrl = new Uri(url);

            var req = (HttpWebRequest)WebRequest.Create(gatewayUrl);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            string formContent = string.Format("ps_store_id={0}&hpp_key={1}&transactionKey={2}",
                                               _monerisPaymentSettings.PsStoreId,
                                               _monerisPaymentSettings.HppKey,
                                               transactionKey);

            req.ContentLength = formContent.Length;
            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
            {
                sw.Write(formContent);
            }

            string response;
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                response = HttpUtility.UrlDecode(sr.ReadToEnd());
            }

            var xmlResponse = new XmlDocument();
            xmlResponse.LoadXml(response);

            var responseSingleNode = xmlResponse.SelectSingleNode("response");
            if (responseSingleNode != null)
            {
                foreach (XmlNode child in responseSingleNode.ChildNodes)
                {
                    values.Add(child.Name, child.InnerText);
                }
            }

            if (values.ContainsKey("response_code"))
            {
                var responseCodeValue = values["response_code"];
                int responseCode;
                if (int.TryParse(responseCodeValue, out responseCode) && responseCode < 50)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                NewPaymentStatus = PaymentStatus.Pending
            };
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var nfi = new CultureInfo("en-US", false).NumberFormat;
            var url = GetPaymentUrl();
            var gatewayUrl = new Uri(url);
            var post = new RemotePost { Url = gatewayUrl.ToString(), Method = "POST" };

            var order = postProcessPaymentRequest.Order;

            //required details
            post.Add("ps_store_id", _monerisPaymentSettings.PsStoreId);
            post.Add("hpp_key", _monerisPaymentSettings.HppKey);
            post.Add("charge_total", order.OrderTotal.ToString(nfi));

            ////other transaction details
            post.Add("cust_id", order.CustomerId.ToString());
            if (!_monerisPaymentSettings.UseSandbox)
            {
                post.Add("order_id", order.Id.ToString());
            }
            post.Add("email", order.BillingAddress.Email);
            post.Add("rvar_order_id", order.Id.ToString());

            //shipping details
            if (order.ShippingAddress != null)
            {
                post.Add("ship_first_name", order.ShippingAddress.FirstName);
                post.Add("ship_last_name", order.ShippingAddress.LastName);
                post.Add("ship_company_name", order.ShippingAddress.Company);
                post.Add("ship_city", order.ShippingAddress.City);
                post.Add("ship_phone", order.ShippingAddress.PhoneNumber);
                post.Add("ship_fax", order.ShippingAddress.FaxNumber);
                post.Add("ship_postal_code", order.ShippingAddress.ZipPostalCode);
                post.Add("ship_address_one",
                         "1: " + order.ShippingAddress.Address1 +
                         " 2: " + order.ShippingAddress.Address2);
                post.Add("ship_state_or_province",
                         order.ShippingAddress.StateProvince != null
                             ? order.ShippingAddress.StateProvince.Name
                             : string.Empty);
                post.Add("ship_country",
                         order.ShippingAddress.Country != null
                             ? order.ShippingAddress.Country.Name
                             : string.Empty);
            }

            //billing details
            if (order.BillingAddress != null)
            {
                post.Add("bill_first_name", order.BillingAddress.FirstName);
                post.Add("bill_last_name", order.BillingAddress.LastName);
                post.Add("bill_company_name", order.BillingAddress.Company);
                post.Add("bill_phone", order.BillingAddress.PhoneNumber);
                post.Add("bill_fax", order.BillingAddress.FaxNumber);
                post.Add("bill_postal_code", order.BillingAddress.ZipPostalCode);
                post.Add("bill_city", order.BillingAddress.City);
                post.Add("bill_address_one",
                         "1: " + order.BillingAddress.Address1 +
                         " 2: " + order.BillingAddress.Address2);
                post.Add("bill_state_or_province",
                         order.BillingAddress.StateProvince != null
                             ? order.BillingAddress.StateProvince.Name
                             : string.Empty);
                post.Add("bill_country",
                         order.BillingAddress.Country != null
                             ? order.BillingAddress.Country.Name
                             : string.Empty);
            }

            post.Post();
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart, IList<ShippingCart> shippingCarts)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart, shippingCarts,
               _monerisPaymentSettings.AdditionalFee, _monerisPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //let's ensure that at least 1 minute passed after order is placed
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1)
                return false;

            return true;
        }

        public Type GetControllerType()
        {
            return typeof(PaymentMonerisController);
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentMoneris";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.Moneris.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentMoneris";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.Moneris.Controllers" }, { "area", null } };
        }

        public override void Install()
        {
            //settings
            var settings = new MonerisPaymentSettings()
            {
                UseSandbox = true,
                AdditionalFeePercentage = false
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.RedirectionTip", "You will be redirected to Moneris site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.AdditionalFeePercentage", "Additinal fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.PsStoreId", "ps_store_id");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.PsStoreId.Hint", "Enter your ps_store_id");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.HppKey", "hpp_key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.Moneris.Fields.HppKey.Hint", "Enter your hpp_key");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<MonerisPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.PsStoreId");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.PsStoreId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.HppKey");
            this.DeletePluginLocaleResource("Plugins.Payments.Moneris.Fields.HppKey.Hint");

            base.Uninstall();
        }

        #endregion

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
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
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
            get { return false; }
        }

        #endregion
    }
}
