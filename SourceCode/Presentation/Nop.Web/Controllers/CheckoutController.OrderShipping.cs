using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Orders;
using Nop.Web.Extensions;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Checkout;

namespace Nop.Web.Controllers
{
    public partial class CheckoutController : BasePublicController
    {
        private List<ShippingCart> GetCurrentShippingCarts()
        {
            return _workContext.CurrentCustomer.ShippingCarts.OrderBy(c => c.RecipientName).ToList();
        }

        private RedirectToRouteResult GetNextShippingCartAction(int recepientId)
        {
            var list = GetCurrentShippingCarts();
            var currentShippingCart = list.FirstOrDefault(c => c.Id == recepientId);
            if (currentShippingCart != null)
            {
                var currentIndex = list.IndexOf(currentShippingCart);
                if (currentIndex < (list.Count - 1))
                {
                    return RedirectToRoute("NEXT_RECIPIENT", new {recepientId = list[currentIndex + 1].Id});
                }
            }
            return RedirectToRoute("CONFIRM_ACTION");
        }

        public ActionResult ShippingAddress(int? recepientId)
        {
            ShippingCart shippingCart;
            if (!recepientId.HasValue)
            {
                shippingCart = GetCurrentShippingCarts().FirstOrDefault();
            }
            else
            {
                shippingCart =
                    _workContext.CurrentCustomer.ShippingCarts.FirstOrDefault(sci => sci.Id == recepientId);
            }
            
            if (shippingCart == null || shippingCart.ShippingCartItems.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            if (!shippingCart.RequiresShipping())
            {
                shippingCart.ShippingAddress = null;
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                return GetNextShippingCartAction(shippingCart.Id);
            }

            //model
            var model = PrepareShippingAddressModel(prePopulateNewAddressWithCustomerFields: true);
            if (shippingCart.RequiresShipping() && shippingCart.ShippingAddress != null)
            {
                model.NewAddress.RecipientTitle = shippingCart.ShippingAddress.RecipientTitle;
                model.NewAddress.RecipientName = shippingCart.ShippingAddress.RecipientName;
                model.NewAddress.Address1 = shippingCart.ShippingAddress.Address1;
                model.NewAddress.Address2 = shippingCart.ShippingAddress.Address2;
                model.NewAddress.PhoneNumber = shippingCart.ShippingAddress.PhoneNumber;
                model.NewAddress.ZipPostalCode = shippingCart.ShippingAddress.ZipPostalCode;
                model.NewAddress.Email = shippingCart.ShippingAddress.Email;

                model.PickUpInStore = shippingCart.PickUpInStore;
                model.ExpectedDeliveryDate = shippingCart.ExpectedDeliveryDate;
                model.ExpectedDeliveryPeriod = shippingCart.ExpectedDeliveryPeriod;

                model.GreetingType = shippingCart.GreetingType;
                model.To = shippingCart.To;
                model.From = shippingCart.From;
                model.Message = shippingCart.Message;
            }
            model.ShippingCart = shippingCart;
            return View(model);
        }

        public ActionResult SelectShippingAddress(int recepientId, int addressId)
        {
            var address = _workContext.CurrentCustomer.Addresses.FirstOrDefault(a => a.Id == addressId);
            if (address == null)
                return RedirectToRoute("CheckoutShippingAddress");

            var shippingCart =
               _workContext.CurrentCustomer.ShippingCarts.FirstOrDefault(sci => sci.Id == recepientId);
            if (shippingCart == null || shippingCart.ShippingCartItems.Count == 0) return RedirectToRoute("ShoppingCart");

            shippingCart.ShippingAddress = address;
            _customerService.UpdateCustomer(_workContext.CurrentCustomer);

            if (_shippingSettings.AllowPickUpInStore)
            {
                //set value indicating that "pick up in store" option has not been chosen
                _genericAttributeService.SaveAttribute(shippingCart, SystemCustomerAttributeNames.SelectedPickUpInStore,
                    false, _storeContext.CurrentStore.Id);
            }

            return GetNextShippingCartAction(shippingCart.Id);
        }

        [HttpPost, ActionName("ShippingCartAddress")]
        [FormValueRequired("nextstep")]
        [ValidateInput(false)]
        public ActionResult NewShippingCartAddress(int recepientId, CheckoutShippingAddressModel model, FormCollection form)
        {
            //validation
            var shippingCart =
               _workContext.CurrentCustomer.ShippingCarts.FirstOrDefault(sci => sci.Id == recepientId);

            if (shippingCart == null || shippingCart.ShippingCartItems.Count == 0)
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return new HttpUnauthorizedResult();

            if (!shippingCart.RequiresShipping())
            {
                shippingCart.ShippingAddress = null;
                _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                return GetNextShippingCartAction(shippingCart.Id);
            }


            //Pick up in store?
            if (_shippingSettings.AllowPickUpInStore)
            {
                if (model.PickUpInStore)
                {
                    //customer decided to pick up in store

                    //no shipping address selected
                    shippingCart.ShippingAddress = null;
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                    //set value indicating that "pick up in store" option has been chosen
                    _genericAttributeService.SaveAttribute(shippingCart, SystemCustomerAttributeNames.SelectedPickUpInStore, true, _storeContext.CurrentStore.Id);

                    //save "pick up in store" shipping method
                    var pickUpInStoreShippingOption = new ShippingOption
                    {
                        Name = _localizationService.GetResource("Checkout.PickUpInStore.MethodName"),
                        Rate = _shippingSettings.PickUpInStoreFee,
                        Description = null,
                        ShippingRateComputationMethodSystemName = null
                    };
                    _genericAttributeService.SaveAttribute(shippingCart,
                        SystemCustomerAttributeNames.SelectedShippingOption,
                        pickUpInStoreShippingOption,
                        _storeContext.CurrentStore.Id);

                    //load next step
                    return GetNextShippingCartAction(shippingCart.Id);
                }

                //set value indicating that "pick up in store" option has not been chosen
                _genericAttributeService.SaveAttribute(shippingCart, SystemCustomerAttributeNames.SelectedPickUpInStore, false, _storeContext.CurrentStore.Id);
            }

            //custom address attributes
            var customAttributes = form.ParseCustomAddressAttributes(_addressAttributeParser, _addressAttributeService);
            var customAttributeWarnings = _addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid)
            {
                //try to find an address with the same values (don't duplicate records)
                var address = _workContext.CurrentCustomer.Addresses.ToList().FindAddress(
                    model.NewAddress.FirstName, model.NewAddress.LastName, model.NewAddress.PhoneNumber,
                    model.NewAddress.Email, model.NewAddress.FaxNumber, model.NewAddress.Company,
                    model.NewAddress.Address1, model.NewAddress.Address2, model.NewAddress.City,
                    model.NewAddress.StateProvinceId, model.NewAddress.ZipPostalCode,
                    model.NewAddress.CountryId, customAttributes,
                    model.NewAddress.RecipientTitle, model.NewAddress.RecipientName);
                if (address == null)
                {
                    address = model.NewAddress.ToEntity();
                    address.CustomAttributes = customAttributes;
                    address.CreatedOnUtc = DateTime.UtcNow;
                    //some validation
                    if (address.CountryId == 0)
                        address.CountryId = null;
                    if (address.StateProvinceId == 0)
                        address.StateProvinceId = null;
                    _workContext.CurrentCustomer.Addresses.Add(address);
                }
                shippingCart.ShippingAddress = address;

                shippingCart.PickUpInStore = model.PickUpInStore;
                shippingCart.ExpectedDeliveryDate = model.ExpectedDeliveryDate;
                shippingCart.ExpectedDeliveryPeriod = model.ExpectedDeliveryPeriod;

                //update Gretting message
                shippingCart.GreetingType = model.GreetingType;
                shippingCart.From = model.From;
                shippingCart.To = model.To;
                shippingCart.Message = model.Message;

                _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                return GetNextShippingCartAction(shippingCart.Id);
            }


            //If we got this far, something failed, redisplay form
            model = PrepareShippingAddressModel(selectedCountryId: model.NewAddress.CountryId);
            return View(model);
        }
    }
}
