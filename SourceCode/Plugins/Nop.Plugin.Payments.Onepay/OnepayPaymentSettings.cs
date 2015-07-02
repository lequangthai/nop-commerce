using Nop.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Onepay
{
    public class OnepayPaymentSettings : ISettings
    {

        public bool UseSandbox { get; set; }
        public string BusinessEmail { get; set; }
        public string PdtToken { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
        public bool PassProductNamesAndTotals { get; set; }
        public bool PdtValidateOrderTotal { get; set; }
        public bool EnableIpn { get; set; }
        public string IpnUrl { get; set; }
        /// <summary>
        /// Enable if a customer should be redirected to the order details page
        /// when he clicks "return to store" link on PayPal site
        /// WITHOUT completing a payment
        /// </summary>
        public bool ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage { get; set; }
        /// <summary>
        /// Enable PayPal address override
        /// </summary>
        public bool AddressOverride { get; set; }


        //-----------------------

        public string vpc_Version { get; set; }

        public string vpc_Currency { get; set; }
        public string vpc_Command { get; set; }
        public string vpc_AccessCode { get; set; }
        public string vpc_Merchant { get; set; }
        public string vpc_Locale { get; set; }
        public string vpc_ReturnURL { get; set; }

        public string vpc_MerchTxnRef { get; set; }

        public string vpc_OrderInfo { get; set; }

        public string vpc_Amount { get; set; }

        public string vpc_TicketNo { get; set; }

        public string AgainLink { get; set; }

        public string Title { get; set; }

        public string vpc_SecureHash { get; set; }


    }
}
