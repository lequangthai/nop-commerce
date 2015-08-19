using Nop.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.EasyPay2
{
    public class EasyPay2PaymentSettings : ISettings
    {
        public TransactMode transactMode { get; set; }

        public string transactionKey { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool additionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal additionalFee { get; set; }

        public String mid { get; set; }

        public String cur { get; set; }

        public String transtype { get; set; }

        public String url { get; set; }
    }
}
