using System;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Security;
using Nop.Services.Customers;
using Nop.Services.GiaHelper;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Models.Newsletter;

namespace Nop.Web.Controllers
{
    public partial class NewsletterController : BaseNopController
    {
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly ICustomerService _customerService;
        private readonly IWorkflowMessageService _workflowMessageService;

        private readonly CustomerSettings _customerSettings;

        public NewsletterController(ILocalizationService localizationService,
            IWorkContext workContext, INewsLetterSubscriptionService newsLetterSubscriptionService, ICustomerService customerService,
            IWorkflowMessageService workflowMessageService, CustomerSettings customerSettings)
        {
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._newsLetterSubscriptionService = newsLetterSubscriptionService;
            this._customerService = customerService;
            this._workflowMessageService = workflowMessageService;

            this._customerSettings = customerSettings;
        }

        [ChildActionOnly]
        public ActionResult NewsletterBox()
        {
            if (_customerSettings.HideNewsletterBlock)
                return Content("");

            return PartialView(new NewsletterBoxModel());
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SubscribeNewsletter(bool subscribe, string email)
        {
            string result;
            bool success = false;

            if (!CommonHelper.IsValidEmail(email))
                result = _localizationService.GetResource("Newsletter.Email.Wrong");
            else
            {
                //subscribe/unsubscribe
                email = email.Trim();

                var subscription = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmail(email);
                if (subscription != null)
                {
                    if (subscribe)
                    {
                        if (!subscription.Active)
                        {
                            _workflowMessageService.SendNewsLetterSubscriptionActivationMessage(subscription, _workContext.WorkingLanguage.Id);
                        }
                        result = _localizationService.GetResource("Newsletter.SubscribeEmailSent");
                    }
                    else
                    {
                        if (subscription.Active)
                        {
                            _workflowMessageService.SendNewsLetterSubscriptionDeactivationMessage(subscription, _workContext.WorkingLanguage.Id);
                        }
                        result = _localizationService.GetResource("Newsletter.UnsubscribeEmailSent");
                    }
                }
                else if (subscribe)
                {
                    subscription = new NewsLetterSubscription()
                    {
                        NewsLetterSubscriptionGuid = Guid.NewGuid(),
                        Email = email,
                        Active = false,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                    _newsLetterSubscriptionService.InsertNewsLetterSubscription(subscription);
                    _workflowMessageService.SendNewsLetterSubscriptionActivationMessageAndCouponCode(subscription, _workContext.WorkingLanguage.Id);

                    result = _localizationService.GetResource("Newsletter.SubscribeEmailSent");
                }
                else
                {
                    result = _localizationService.GetResource("Newsletter.UnsubscribeEmailSent");
                }
                success = true;
            }

            return Json(new
            {
                Success = success,
                Result = result,
            });
        }

        public ActionResult SubscriptionActivation(Guid token, bool active)
        {
            var subscription = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByGuid(token);
            if (subscription == null)
                return RedirectToRoute("HomePage");

            var model = new SubscriptionActivationModel();

            if (active)
            {
                subscription.Active = active;
                _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscription);


                var newsletterRegisteredRole = _customerService.GetCustomerRoleBySystemName(GiaConstance.NewsLetterRegisterdRoleName);
                if (newsletterRegisteredRole == null)
                {
                    newsletterRegisteredRole = new CustomerRole
                                                   {
                                                       Name = GiaConstance.NewsLetterRegisterdRoleName,
                                                       SystemName = GiaConstance.NewsLetterRegisterdRoleName,
                                                       Active = true
                                                   };

                    _customerService.InsertCustomerRole(newsletterRegisteredRole);
                }

                var customer = _customerService.GetCustomerByEmail(subscription.Email);
                if (customer != null)
                {
                    if (customer.CustomerRoles.FirstOrDefault(c => c.Id == newsletterRegisteredRole.Id) == null)
                    {
                        customer.CustomerRoles.Add(newsletterRegisteredRole);
                    }
                }
            }
            else
            {
                //_newsLetterSubscriptionService.DeleteNewsLetterSubscription(subscription);

                subscription.Active = false;
                _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscription);
            }

            if (active)
                model.Result = _localizationService.GetResource("Newsletter.ResultActivated");
            else
                model.Result = _localizationService.GetResource("Newsletter.ResultDeactivated");

            return View(model);
        }
    }
}
