using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Web.Extensions;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Web.Controllers
{
    public partial class ShoppingCartController : BasePublicController
    {
        private const string MySelf = "myself";
        private const decimal OrderTotalValueForFreeShipping = 150;
        private const decimal ShippingFee = 15;

        #region Ultities
        [NonAction]
        protected virtual void PrepareShippingCartModel(ShoppingCartModel model,
            IList<ShoppingCartItem> cart,
            IList<ShippingCart> shippingCarts,
            IList<ShippingCartItem> shippingCartItems, 
            bool isEditable = true,
            bool validateCheckoutAttributes = false,
            bool prepareEstimateShippingIfEnabled = true, bool setEstimateShippingDefaultAddress = true,
            bool prepareAndDisplayOrderReviewData = false)
        {
            if (cart == null)
                throw new ArgumentNullException("cart");

            if (model == null)
                throw new ArgumentNullException("model");

            model.OnePageCheckoutEnabled = _orderSettings.OnePageCheckoutEnabled;

            if (cart.Count == 0)
                return;

            #region Simple properties

            model.IsEditable = isEditable;
            model.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
            model.ShowSku = _catalogSettings.ShowProductSku;
            var checkoutAttributesXml = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, _genericAttributeService, _storeContext.CurrentStore.Id);
            model.CheckoutAttributeInfo = _checkoutAttributeFormatter.FormatAttributes(checkoutAttributesXml, _workContext.CurrentCustomer);
            bool minOrderSubtotalAmountOk = _orderProcessingService.ValidateMinOrderSubtotalAmount(cart);
            if (!minOrderSubtotalAmountOk)
            {
                decimal minOrderSubtotalAmount = _currencyService.ConvertFromPrimaryStoreCurrency(_orderSettings.MinOrderSubtotalAmount, _workContext.WorkingCurrency);
                model.MinOrderSubtotalWarning = string.Format(_localizationService.GetResource("Checkout.MinOrderSubtotalAmount"), _priceFormatter.FormatPrice(minOrderSubtotalAmount, true, false));
            }
            model.TermsOfServiceOnShoppingCartPage = _orderSettings.TermsOfServiceOnShoppingCartPage;
            model.TermsOfServiceOnOrderConfirmPage = _orderSettings.TermsOfServiceOnOrderConfirmPage;

            //gift card and gift card boxes
            model.DiscountBox.Display = _shoppingCartSettings.ShowDiscountBox;
            var discountCouponCode = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.DiscountCouponCode);
            var discount = _discountService.GetDiscountByCouponCode(discountCouponCode);
            if (discount != null &&
                discount.RequiresCouponCode &&
                _discountService.IsDiscountValid(discount, _workContext.CurrentCustomer))
                model.DiscountBox.CurrentCode = discount.CouponCode;
            model.GiftCardBox.Display = _shoppingCartSettings.ShowGiftCardBox;

            //cart warnings
            var cartWarnings = _shoppingCartService.GetShoppingCartWarnings(cart, checkoutAttributesXml, validateCheckoutAttributes);
            foreach (var warning in cartWarnings)
                model.Warnings.Add(warning);

            #endregion

            #region Checkout attributes

            var checkoutAttributes = _checkoutAttributeService.GetAllCheckoutAttributes(_storeContext.CurrentStore.Id, !cart.RequiresShipping());
            foreach (var attribute in checkoutAttributes)
            {
                var attributeModel = new ShoppingCartModel.CheckoutAttributeModel
                {
                    Id = attribute.Id,
                    Name = attribute.GetLocalized(x => x.Name),
                    TextPrompt = attribute.GetLocalized(x => x.TextPrompt),
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                    DefaultValue = attribute.DefaultValue
                };
                if (!String.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
                {
                    attributeModel.AllowedFileExtensions = attribute.ValidationFileAllowedExtensions
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = _checkoutAttributeService.GetCheckoutAttributeValues(attribute.Id);
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new ShoppingCartModel.CheckoutAttributeValueModel
                        {
                            Id = attributeValue.Id,
                            Name = attributeValue.GetLocalized(x => x.Name),
                            ColorSquaresRgb = attributeValue.ColorSquaresRgb,
                            IsPreSelected = attributeValue.IsPreSelected,
                        };
                        attributeModel.Values.Add(attributeValueModel);

                        //display price if allowed
                        if (_permissionService.Authorize(StandardPermissionProvider.DisplayPrices))
                        {
                            decimal priceAdjustmentBase = _taxService.GetCheckoutAttributePrice(attributeValue);
                            decimal priceAdjustment = _currencyService.ConvertFromPrimaryStoreCurrency(priceAdjustmentBase, _workContext.WorkingCurrency);
                            if (priceAdjustmentBase > decimal.Zero)
                                attributeValueModel.PriceAdjustment = "+" + _priceFormatter.FormatPrice(priceAdjustment);
                            else if (priceAdjustmentBase < decimal.Zero)
                                attributeValueModel.PriceAdjustment = "-" + _priceFormatter.FormatPrice(-priceAdjustment);
                        }
                    }
                }



                //set already selected attributes
                var selectedCheckoutAttributes = _workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, _genericAttributeService, _storeContext.CurrentStore.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.Checkboxes:
                    case AttributeControlType.ColorSquares:
                        {
                            if (!String.IsNullOrEmpty(selectedCheckoutAttributes))
                            {
                                //clear default selection
                                foreach (var item in attributeModel.Values)
                                    item.IsPreSelected = false;

                                //select new values
                                var selectedValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(selectedCheckoutAttributes);
                                foreach (var attributeValue in selectedValues)
                                    foreach (var item in attributeModel.Values)
                                        if (attributeValue.Id == item.Id)
                                            item.IsPreSelected = true;
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //do nothing
                            //values are already pre-set
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            if (!String.IsNullOrEmpty(selectedCheckoutAttributes))
                            {
                                var enteredText = _checkoutAttributeParser.ParseValues(selectedCheckoutAttributes, attribute.Id);
                                if (enteredText.Count > 0)
                                    attributeModel.DefaultValue = enteredText[0];
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                        {
                            //keep in mind my that the code below works only in the current culture
                            var selectedDateStr = _checkoutAttributeParser.ParseValues(selectedCheckoutAttributes, attribute.Id);
                            if (selectedDateStr.Count > 0)
                            {
                                DateTime selectedDate;
                                if (DateTime.TryParseExact(selectedDateStr[0], "D", CultureInfo.CurrentCulture,
                                                       DateTimeStyles.None, out selectedDate))
                                {
                                    //successfully parsed
                                    attributeModel.SelectedDay = selectedDate.Day;
                                    attributeModel.SelectedMonth = selectedDate.Month;
                                    attributeModel.SelectedYear = selectedDate.Year;
                                }
                            }

                        }
                        break;
                    default:
                        break;
                }

                model.CheckoutAttributes.Add(attributeModel);
            }

            #endregion

            #region Cart items
            foreach (var sci in cart)
            {
                var cartItemModel = new ShoppingCartModel.ShoppingCartItemModel
                {
                    Id = sci.Id,
                    Sku = sci.Product.FormatSku(sci.AttributesXml, _productAttributeParser),
                    ProductId = sci.Product.Id,
                    ProductName = sci.Product.GetLocalized(x => x.Name),
                    ProductSeName = sci.Product.GetSeName(),
                    Quantity = sci.Quantity,
                    AttributeInfo = _productAttributeFormatter.FormatAttributes(sci.Product, sci.AttributesXml),
                };

                //allow editing?
                //1. setting enabled?
                //2. simple product?
                //3. has attribute or gift card?
                //4. visible individually?
                cartItemModel.AllowItemEditing = _shoppingCartSettings.AllowCartItemEditing &&
                    sci.Product.ProductType == ProductType.SimpleProduct &&
                    (!String.IsNullOrEmpty(cartItemModel.AttributeInfo) || sci.Product.IsGiftCard) &&
                    sci.Product.VisibleIndividually;

                //allowed quantities
                var allowedQuantities = sci.Product.ParseAllowedQuantities();
                foreach (var qty in allowedQuantities)
                {
                    cartItemModel.AllowedQuantities.Add(new SelectListItem
                    {
                        Text = qty.ToString(),
                        Value = qty.ToString(),
                        Selected = sci.Quantity == qty
                    });
                }

                //recurring info
                if (sci.Product.IsRecurring)
                    cartItemModel.RecurringInfo = string.Format(_localizationService.GetResource("ShoppingCart.RecurringPeriod"), sci.Product.RecurringCycleLength, sci.Product.RecurringCyclePeriod.GetLocalizedEnum(_localizationService, _workContext));

                //rental info
                if (sci.Product.IsRental)
                {
                    var rentalStartDate = sci.RentalStartDateUtc.HasValue ? sci.Product.FormatRentalDate(sci.RentalStartDateUtc.Value) : "";
                    var rentalEndDate = sci.RentalEndDateUtc.HasValue ? sci.Product.FormatRentalDate(sci.RentalEndDateUtc.Value) : "";
                    cartItemModel.RentalInfo = string.Format(_localizationService.GetResource("ShoppingCart.Rental.FormattedDate"),
                        rentalStartDate, rentalEndDate);
                }

                //unit prices
                if (sci.Product.CallForPrice)
                {
                    cartItemModel.UnitPrice = _localizationService.GetResource("Products.CallForPrice");
                }
                else
                {
                    decimal taxRate;
                    decimal shoppingCartUnitPriceWithDiscountBase = _taxService.GetProductPrice(sci.Product, _priceCalculationService.GetUnitPrice(sci), out taxRate);
                    decimal shoppingCartUnitPriceWithDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartUnitPriceWithDiscountBase, _workContext.WorkingCurrency);
                    cartItemModel.UnitPrice = _priceFormatter.FormatPrice(shoppingCartUnitPriceWithDiscount);
                }
                //subtotal, discount
                if (sci.Product.CallForPrice)
                {
                    cartItemModel.SubTotal = _localizationService.GetResource("Products.CallForPrice");
                }
                else
                {
                    //sub total
                    Discount scDiscount;
                    decimal shoppingCartItemDiscountBase;
                    decimal taxRate;
                    decimal shoppingCartItemSubTotalWithDiscountBase = _taxService.GetProductPrice(sci.Product, _priceCalculationService.GetSubTotal(sci, true, out shoppingCartItemDiscountBase, out scDiscount), out taxRate);
                    decimal shoppingCartItemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartItemSubTotalWithDiscountBase, _workContext.WorkingCurrency);
                    cartItemModel.SubTotal = _priceFormatter.FormatPrice(shoppingCartItemSubTotalWithDiscount);

                    //display an applied discount amount
                    if (scDiscount != null)
                    {
                        shoppingCartItemDiscountBase = _taxService.GetProductPrice(sci.Product, shoppingCartItemDiscountBase, out taxRate);
                        if (shoppingCartItemDiscountBase > decimal.Zero)
                        {
                            decimal shoppingCartItemDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartItemDiscountBase, _workContext.WorkingCurrency);
                            cartItemModel.Discount = _priceFormatter.FormatPrice(shoppingCartItemDiscount);
                        }
                    }
                }

                //picture
                if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                {
                    cartItemModel.Picture = PrepareCartItemPictureModel(sci,
                        _mediaSettings.CartThumbPictureSize, true, cartItemModel.ProductName);
                }

                //item warnings
                var itemWarnings = _shoppingCartService.GetShoppingCartItemWarnings(
                    _workContext.CurrentCustomer,
                    sci.ShoppingCartType,
                    sci.Product,
                    sci.StoreId,
                    sci.AttributesXml,
                    sci.CustomerEnteredPrice,
                    sci.RentalStartDateUtc,
                    sci.RentalEndDateUtc,
                    sci.Quantity,
                    false);
                foreach (var warning in itemWarnings)
                    cartItemModel.Warnings.Add(warning);

                //shipping item
                var existsShippingItems = shippingCartItems.Where(c => c.ShoppingCartItemId == sci.Id).ToList();
                for (int i = 0; i < sci.Quantity; i++)
                {
                    var existsShippingItem = existsShippingItems.FirstOrDefault(c => c.ShippingCartPosition == i);
                    var shippingCartItemModel = new ShoppingCartModel.ShippingCartItemModel
                    {
                        RecipientName = existsShippingItem != null ? existsShippingItem.ShippingCart.RecipientName : string.Empty,
                        ShippingCartPosition = i
                    };
                    cartItemModel.ShippingCartItems.Add(shippingCartItemModel);
                }
                
                model.Items.Add(cartItemModel);
            }

            #endregion

            #region ShippingCartItems
            model.RecipientNames = shippingCarts.Select(c => c.RecipientName).ToList();
            if (model.RecipientNames.Contains(MySelf) && model.RecipientNames.IndexOf(MySelf) != 0)
            {
                model.RecipientNames.Remove(MySelf);
            }
            if (!model.RecipientNames.Contains(MySelf))
            {
                model.RecipientNames.Insert(0, MySelf);
            }

            #endregion

            #region Button payment methods

            var paymentMethods = _paymentService
                .LoadActivePaymentMethods(_workContext.CurrentCustomer.Id, _storeContext.CurrentStore.Id)
                .Where(pm => pm.PaymentMethodType == PaymentMethodType.Button)
                .Where(pm => !pm.HidePaymentMethod(cart))
                .ToList();
            foreach (var pm in paymentMethods)
            {
                if (cart.IsRecurring() && pm.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                string actionName;
                string controllerName;
                RouteValueDictionary routeValues;
                pm.GetPaymentInfoRoute(out actionName, out controllerName, out routeValues);

                model.ButtonPaymentMethodActionNames.Add(actionName);
                model.ButtonPaymentMethodControllerNames.Add(controllerName);
                model.ButtonPaymentMethodRouteValues.Add(routeValues);
            }

            #endregion

            #region Order review data

            if (prepareAndDisplayOrderReviewData)
            {
                model.OrderReviewData.Display = true;

                //billing info
                var billingAddress = _workContext.CurrentCustomer.BillingAddress;
                if (billingAddress != null)
                    model.OrderReviewData.BillingAddress.PrepareModel(
                        address: billingAddress,
                        excludeProperties: false,
                        addressSettings: _addressSettings,
                        addressAttributeFormatter: _addressAttributeFormatter);

                //shipping info
                if (cart.RequiresShipping())
                {
                    model.OrderReviewData.IsShippable = true;

                    if (_shippingSettings.AllowPickUpInStore)
                    {
                        model.OrderReviewData.SelectedPickUpInStore = _workContext.CurrentCustomer.GetAttribute<bool>(SystemCustomerAttributeNames.SelectedPickUpInStore, _storeContext.CurrentStore.Id);
                    }

                    if (!model.OrderReviewData.SelectedPickUpInStore)
                    {
                        var shippingAddress = _workContext.CurrentCustomer.ShippingAddress;
                        if (shippingAddress != null)
                        {
                            model.OrderReviewData.ShippingAddress.PrepareModel(
                                address: shippingAddress,
                                excludeProperties: false,
                                addressSettings: _addressSettings,
                                addressAttributeFormatter: _addressAttributeFormatter);
                        }
                    }


                    //selected shipping method
                    var shippingOption = _workContext.CurrentCustomer.GetAttribute<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, _storeContext.CurrentStore.Id);
                    if (shippingOption != null)
                        model.OrderReviewData.ShippingMethod = shippingOption.Name;
                }
                //payment info
                var selectedPaymentMethodSystemName = _workContext.CurrentCustomer.GetAttribute<string>(
                    SystemCustomerAttributeNames.SelectedPaymentMethod, _storeContext.CurrentStore.Id);
                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(selectedPaymentMethodSystemName);
                model.OrderReviewData.PaymentMethod = paymentMethod != null ? paymentMethod.GetLocalizedFriendlyName(_localizationService, _workContext.WorkingLanguage.Id) : "";

                //custom values
                var processPaymentRequest = _httpContext.Session["OrderPaymentInfo"] as ProcessPaymentRequest;
                if (processPaymentRequest != null)
                {
                    model.OrderReviewData.CustomValues = processPaymentRequest.CustomValues;
                }
            }
            #endregion
        }

        #endregion

        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult ShippingCart()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();
            var shippingCarts = _workContext.CurrentCustomer.ShippingCarts.OrderBy(c=>c.RecipientName).ToList();
            var shippingCartItems = new List<ShippingCartItem>();
            foreach (var shippingCart in shippingCarts)
            {
                shippingCartItems.AddRange(shippingCart.ShippingCartItems);
            }
            var model = new ShoppingCartModel();
            PrepareShippingCartModel(model, cart, shippingCarts, shippingCartItems);
            return View(model);
        }

        [ValidateInput(false)]
        [HttpPost, ActionName("ShippingCart")]
        [FormValueRequired("updateshippingcart")]
        public ActionResult UpdateShippingCart(FormCollection form)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
                return RedirectToRoute("HomePage");

            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();

            var allIdsToRemove = form["removefromcart"] != null ? form["removefromcart"].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList() : new List<int>();
            var formKeys = form.AllKeys.ToList();

            //current warnings <cart item identifier, warnings>
            var innerWarnings = new Dictionary<int, IList<string>>();
            foreach (var sci in cart)
            {
                bool remove = allIdsToRemove.Contains(sci.Id);
                if (remove)
                    _shoppingCartService.DeleteShoppingCartItem(sci, ensureOnlyActiveCheckoutAttributes: true);
                else
                {
                    var formQuantityKey = string.Format("itemquantity{0}", sci.Id);
                    if (formKeys.Any(c => c.Equals(formQuantityKey, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        int newQuantity;
                        if (int.TryParse(form[formQuantityKey], out newQuantity))
                        {
                            var currSciWarnings =
                                _shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
                                    sci.Id, sci.AttributesXml, sci.CustomerEnteredPrice,
                                    sci.RentalStartDateUtc, sci.RentalEndDateUtc,
                                    newQuantity, true);
                            innerWarnings.Add(sci.Id, currSciWarnings);
                        }
                    }
                    var formReceipientKey = string.Format("recipient{0}", sci.Id);
                    if (formKeys.Any(c => c.Equals(formReceipientKey, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var reciepientNames = form[formReceipientKey].Split(',').ToList();
                        for (int i = 0; i < reciepientNames.Count; i++)
                        {
                            var recipientName = reciepientNames[i];
                            if (!string.IsNullOrEmpty(recipientName))
                            {
                                _shoppingCartService.UpdateShippingCart(_storeContext.CurrentStore.Id,
                                _workContext.CurrentCustomer, sci.Id, i, recipientName);
                            }
                        }
                    }
                    _shoppingCartService.DeleteShippingCartItems(_storeContext.CurrentStore.Id, _workContext.CurrentCustomer, sci.Id);
                }
            }

            //updated cart
            cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();
            var shippingCarts = _workContext.CurrentCustomer.ShippingCarts.OrderBy(c => c.RecipientName).ToList();
            var shippingCartItems = new List<ShippingCartItem>();
            foreach (var shippingCart in shippingCarts)
            {
                shippingCartItems.AddRange(shippingCart.ShippingCartItems);
            }
            var model = new ShoppingCartModel();
            PrepareShippingCartModel(model, cart, shippingCarts, shippingCartItems);
            //update current warnings
            foreach (var kvp in innerWarnings)
            {
                //kvp = <cart item identifier, warnings>
                var sciId = kvp.Key;
                var warnings = kvp.Value;
                //find model
                var sciModel = model.Items.FirstOrDefault(x => x.Id == sciId);
                if (sciModel != null)
                    foreach (var w in warnings)
                        if (!sciModel.Warnings.Contains(w))
                            sciModel.Warnings.Add(w);
            }
            return View(model);
        }

        public ActionResult ShippingOrderReview()
        {
            var model = BuildShippingOrderModel();
            return PartialView(model);
        }

        [HttpPost, ActionName("ShippingCart")]
        [FormValueRequired("updateshippingcart")]
        public ActionResult ShippingOrderConfirm(bool isAcceptTerm)
        {
            if (isAcceptTerm)
            {
                return RedirectToRoute("PAYMENT_PROCESS");
            }
            var model = BuildShippingOrderModel();
            return PartialView(model);
        }

        [ChildActionOnly]
        public ActionResult ShippingOrderTotals(bool isEditable)
        {
            var model = BuildShippingOrderModel();
            return PartialView(model);
        }

        private ShippingOrderModel BuildShippingOrderModel()
        {
            var shoppingCartItems = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                .LimitPerStore(_storeContext.CurrentStore.Id)
                .ToList();
            var shippingCartItems = new List<ShippingCartItem>();
            shippingCartItems = _workContext.CurrentCustomer.ShippingCarts.Aggregate(shippingCartItems,
                (current, shippingCart) => current.Union(shippingCart.ShippingCartItems).ToList());
            var shippingCarts = shippingCartItems.Select(c => c.ShippingCart).Distinct().ToList();
            var model = new ShippingOrderModel();
            foreach (var shippingCart in _workContext.CurrentCustomer.ShippingCarts)
            {
                model.ShippingOrderItems.Add(BuildShippingOrderItemModel(shippingCart, shippingCartItems, shoppingCartItems));
            }
            model.OrderTotalsModel = PrepareOrderTotalsModel(shoppingCartItems, shippingCarts,  false);
            //add shippingFee of each shipment
            var currentShippingCarts =
                _workContext.CurrentCustomer.ShippingCarts.Where(
                    shippingCart =>
                        shippingCart.ShippingCartItems.Any(
                            shippingCartItem =>
                                shoppingCartItems.Any(
                                    shoppingCarItem => shoppingCarItem.Id == shippingCartItem.ShoppingCartItemId)));
            foreach (var shippingCart in currentShippingCarts)
            {
                model.OrderTotalsModel.OrderTotalValue += shippingCart.ShippingFee;
            }
            return model;
        }

        private ShippingOrderItemModel BuildShippingOrderItemModel(ShippingCart shippingCart,
            List<ShippingCartItem> shippingCartItems, List<ShoppingCartItem> shoppingCartItems)
        {
            var model = new ShippingOrderItemModel
            {
                ShippingCart = shippingCart,
                ShoppingCartModel = new ShoppingCartModel()
            };
            var cart = new List<ShoppingCartItem>();
            foreach (var shoppingCartItem in shoppingCartItems)
            {
                var count =
                    shippingCartItems.Count(
                        c => c.ShippingCartId == shippingCart.Id && c.ShoppingCartItemId == shoppingCartItem.Id);
                if (count > 0)
                {
                    var modelItem = shoppingCartItem.CloneViaJson();
                    modelItem.Quantity = count;
                    cart.Add(modelItem);
                }
            }
            PrepareShoppingCartModel(model.ShoppingCartModel, cart, false, false, false, false);
            model.OrderTotalsModel = PrepareOrderTotalsModel(cart, new List<ShippingCart>(),  false);
            if (model.OrderTotalsModel.OrderTotalValue >= OrderTotalValueForFreeShipping)
            {
                shippingCart.ShippingFee = 0;
            }
            else
            {
                shippingCart.ShippingFee = ShippingFee;
            }
            return model;
        }
    }

    public class ShippingOrderModel
    {
        public ShippingOrderModel()
        {
            ShippingOrderItems = new List<ShippingOrderItemModel>();
        }

        public List<ShippingOrderItemModel> ShippingOrderItems { get; set; }

        public OrderTotalsModel OrderTotalsModel { get; set; }
    }

    public class ShippingOrderItemModel
    {
        public ShippingCart ShippingCart { get; set; }

        public ShoppingCartModel ShoppingCartModel { get; set; }

        public OrderTotalsModel OrderTotalsModel { get; set; }
    }
}
