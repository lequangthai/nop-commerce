using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.EasyPay2.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPay2.Fields.mid")]
        public String mid { get; set; }

        public bool mid_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPay2.Fields.cur")]
        public String cur { get; set; }

        public bool cur_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPay2.Fields.transtype")]
        public String transtype { get; set; }

        public bool transtype_OverrideForStore { get; set; }

        public int TransactModeId { get; set; }
        public bool TransactModeId_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.EasyPay2.Fields.TransactModeValues")]
        public SelectList TransactModeValues { get; set; }

        [NopResourceDisplayName("Plugins.Payments.EasyPay2.Fields.url")]
        public String url { get; set; }

        public bool url_OverrideForStore { get; set; }

    }
}
