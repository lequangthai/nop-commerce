using Nop.Core;
using Nop.Core.Domain.Orders;
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Nop.Web.Framework;

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
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var easyPay2PaymentSettings = _settingService.LoadSetting<EasyPay2PaymentSettings>(storeScope);
            var model = new ConfigurationModel();
            model.mid = easyPay2PaymentSettings.mid;
            model.cur= easyPay2PaymentSettings.cur;
            model.transtype = easyPay2PaymentSettings.transtype;
            model.ActiveStoreScopeConfiguration = storeScope;
            model.TransactModeId = Convert.ToInt32(easyPay2PaymentSettings.transactMode);
            model.TransactModeValues = easyPay2PaymentSettings.transactMode.ToSelectList();
            model.url = easyPay2PaymentSettings.url;
            if (storeScope > 0)
            {
                model.mid_OverrideForStore = _settingService.SettingExists(easyPay2PaymentSettings, x => x.mid, storeScope);
                model.cur_OverrideForStore = _settingService.SettingExists(easyPay2PaymentSettings, x => x.cur, storeScope);
                model.transtype_OverrideForStore = _settingService.SettingExists(easyPay2PaymentSettings, x => x.transtype, storeScope);
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(easyPay2PaymentSettings, x => x.transactMode, storeScope);
                model.url_OverrideForStore = _settingService.SettingExists(easyPay2PaymentSettings, x => x.url, storeScope);
            }
            return View("~/Plugins/Payments.EasyPay2/Views/PaymentEasyPay2/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var easyPay2PaymentSettings = _settingService.LoadSetting<EasyPay2PaymentSettings>(storeScope);

            //save settings
            easyPay2PaymentSettings.mid = model.mid;
            easyPay2PaymentSettings.cur = model.cur;
            easyPay2PaymentSettings.transtype = model.transtype;
            easyPay2PaymentSettings.transactMode = (TransactMode)model.TransactModeId;
            easyPay2PaymentSettings.url = model.url;

            if (model.mid_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPay2PaymentSettings, x => x.mid, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPay2PaymentSettings, x => x.mid, storeScope);

            if (model.cur_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPay2PaymentSettings, x => x.cur, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPay2PaymentSettings, x => x.cur, storeScope);

            if (model.transtype_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPay2PaymentSettings, x => x.transtype, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPay2PaymentSettings, x => x.transtype, storeScope);

            if (model.TransactModeId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPay2PaymentSettings, x => x.transactMode, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPay2PaymentSettings, x => x.transactMode, storeScope);

            if (model.url_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(easyPay2PaymentSettings, x => x.url, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(easyPay2PaymentSettings, x => x.url, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        { 
            return View("~/Plugins/Payments.EasyPay2/Views/PaymentEasyPay2/PaymentInfo.cshtml");
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

        [ValidateInput(false)]
        public ActionResult statusHandler()
        {
            Guid orderNumberGuid = Guid.Empty;
            if ("statusUrl".Equals(Request.Params.GetValues("type")[0]))
            {
                byte[] param = Request.BinaryRead(Request.ContentLength);
                string strRequest = Encoding.ASCII.GetString(param);
                Debug.WriteLine(Request.Params);
                var form = this.Request.Form;
                //var newPaymentStatus = PaymentStatus.Pending;
                var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.EasyPay2") as EasyPay2PaymentProcessor;
                if (processor == null ||
                    !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                    throw new NopException("PayPal Standard module cannot be loaded");
                
                try{
                    orderNumberGuid = new Guid(form.GetValues("TM_RefNo")[0]);
                    var order = _orderService.GetOrderByGuid(orderNumberGuid);
                    //Order note
                    var sb = new StringBuilder();
                    //sb.AppendLine("TM_MCode: " + form.GetValues("TM_MCode")[0]);
                    //sb.AppendLine("TM_RefNo: " + form.GetValues("TM_RefNo")[0]);
                    sb.AppendLine(form.GetValues("TM_Status")[0]);
                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = sb.ToString(),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = order.Id
                    });
                    _orderService.UpdateOrder(order);
                    //set status payment
                    if ("YES".Equals(form.GetValues("TM_Status")[0]))
                    {
                        if ("auth".Equals(form.GetValues("TM_TrnType")[0]))
                        {
                            //newPaymentStatus = PaymentStatus.Authorized;
                            //mark order as Authorized
                            if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
                            {
                                _orderProcessingService.MarkAsAuthorized(order);
                            }
                            _logger.Information("Success !!!. PaymentStatus: Authorized");
                        }
                        else if ("sale".Equals(form.GetValues("TM_TrnType")[0]))
                        {
                            //newPaymentStatus = PaymentStatus.Paid;
                            //mark order as paid
                            if (_orderProcessingService.CanMarkOrderAsPaid(order))
                            {
                                _orderService.UpdateOrder(order);
                                _orderProcessingService.MarkOrderAsPaid(order);
                            }
                            _logger.Information("Success !!!. PaymentStatus: PAID");
                        }
                        //return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                    }
                    else if ("NO".Equals(form.GetValues("TM_Status")[0]))
                    {
                        _logger.Error(form.GetValues("TM_ErrorMsg")[0]);
                        //return RedirectToAction("Index", "Home", new { area = "" });
                    }
                }
                catch (Exception e) {
                    _logger.Error(e.Message + " " + e.StackTrace);
                    ErrorNotification(e.Message);
                }
            }
            else
            {
                 orderNumberGuid = new Guid(Request.Params.GetValues("orderGuid")[0]);
                 var order = _orderService.GetOrderByGuid(orderNumberGuid);
                //return url
                 if (order.PaymentStatus == PaymentStatus.Authorized || order.PaymentStatus == PaymentStatus.Paid)
                 {
                     _logger.Information(order.OrderNotes.ToString());
                     return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                 }
                 else {
                     return RedirectToAction("Index", "Home", new { area = "" });
                 }
            }

            return RedirectToAction("Index", "Home", new { area = "" });
            //if (order != null)
            //{
            //    var sb = new StringBuilder();
            //    //sb.AppendLine("TM_MCode: " + form.GetValues("TM_MCode")[0]);
            //    //sb.AppendLine("TM_RefNo: " + form.GetValues("TM_RefNo")[0]);
            //    //sb.AppendLine("TM_Currency: " + form.GetValues("TM_Currency")[0]);
            //    //sb.AppendLine("TM_DebitAmt: " + form.GetValues("TM_DebitAmt")[0]);
            //    sb.AppendLine("TM_Status: " + form.GetValues("TM_Status")[0]);
            //    //sb.AppendLine("TM_ErrorMsg: " + form.GetValues("TM_ErrorMsg")[0]);
            //    //sb.AppendLine("TM_PaymentType: " + form.GetValues("TM_PaymentType")[0]);
            //    //sb.AppendLine("TM_ApprovalCode: " + form.GetValues("TM_ApprovalCode")[0]);
            //    //sb.AppendLine("TM_BankRespCode: " + form.GetValues("TM_BankRespCode")[0]);
            //    //sb.AppendLine("TM_Error: " + form.GetValues("TM_Error")[0]);
            //    //sb.AppendLine("TM_UserField1: " + form.GetValues("TM_UserField1")[0]);
            //    //sb.AppendLine("TM_UserField2: " + form.GetValues("TM_UserField2")[0]);
            //    //sb.AppendLine("TM_UserField3: " + form.GetValues("TM_UserField3")[0]);
            //    //sb.AppendLine("TM_UserField4: " + form.GetValues("TM_UserField4")[0]);
            //    //sb.AppendLine("TM_UserField5: " + form.GetValues("TM_UserField5")[0]);
            //    //sb.AppendLine("TM_CCLast4Digit: " + form.GetValues("TM_CCLast4Digit")[0]);
            //    //sb.AppendLine("TM_ExpiryDate: " + form.GetValues("TM_ExpiryDate")[0]);
            //    //sb.AppendLine("TM_TrnType: " + form.GetValues("TM_TrnType")[0]);
            //    //sb.AppendLine("TM_SubTrnType: " + form.GetValues("TM_SubTrnType")[0]);
            //    //sb.AppendLine("TM_CCNum: " + form.GetValues("TM_CCNum")[0]);
            //    //sb.AppendLine("TM_IPP_FirstPayment: " + form.GetValues("TM_IPP_FirstPayment")[0]);
            //    //sb.AppendLine("TM_IPP_LastPayment: " + form.GetValues("TM_IPP_LastPayment")[0]);
            //    //sb.AppendLine("TM_IPP_MonthlyPayment: " + form.GetValues("TM_IPP_MonthlyPayment")[0]);
            //    //sb.AppendLine("TM_IPP_TransTenure: " + form.GetValues("TM_IPP_TransTenure")[0]);
            //    //sb.AppendLine("TM_IPP_TotalInterest: " + form.GetValues("TM_IPP_TotalInterest")[0]);
            //    //sb.AppendLine("TM_IPP_DownPayment: " + form.GetValues("TM_IPP_DownPayment")[0]);
            //    //sb.AppendLine("TM_IPP_MonthlyInterest: " + form.GetValues("TM_IPP_MonthlyInterest")[0]);
            //    //sb.AppendLine("TM_Original_RefNo: " + form.GetValues("TM_Original_RefNo")[0]);
            //    //sb.AppendLine("TM_Signature: " + form.GetValues("TM_Signature")[0]);
            //    //sb.AppendLine("TM_OriginalPayType: " + form.GetValues("TM_OriginalPayType")[0]);

            //    //order note
            //    order.OrderNotes.Add(new OrderNote
            //    {
            //        Note = sb.ToString(),
            //        DisplayToCustomer = false,
            //        CreatedOnUtc = DateTime.UtcNow,
            //        OrderId = order.Id,
                    
            //    });
            //    _orderService.UpdateOrder(order);

            //    if ("YES".Equals(form.GetValues("TM_Status")[0]))
            //    {
            //        if ("auth".Equals(form.GetValues("TM_TrnType")[0]))
            //        {
            //            newPaymentStatus = PaymentStatus.Authorized;
            //        }
            //        else if ("sale".Equals(form.GetValues("TM_TrnType")[0])) {
            //            newPaymentStatus = PaymentStatus.Paid;
            //        }
                    
            //        _logger.Information("Success !!!. PaymentStatus: PAID");
            //    }
            //    else if ("NO".Equals(form.GetValues("TM_Status")[0]))
            //    {
            //         _logger.Error(form.GetValues("TM_ErrorMsg")[0]);
            //         return RedirectToAction("Index", "Home", new { area = "" });
            //    }

            //    //mark order as paid
            //    if (newPaymentStatus == PaymentStatus.Paid)
            //    {
            //        if (_orderProcessingService.CanMarkOrderAsPaid(order))
            //        {
            //            _orderService.UpdateOrder(order);
            //            _orderProcessingService.MarkOrderAsPaid(order);
            //        }
            //    }
            //    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            //}
            //else {
            //    //return RedirectToRoute("CheckoutCompleted", new { orderId = Request.Params.GetValues("orderId")[0] });
            //    return RedirectToAction("Index", "Home", new { area = "" });
            //}
        }

    }
}
