using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Discounts;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Tax;

namespace Nop.Services.Orders
{
    /// <summary>
    /// Order service
    /// </summary>
    public partial class OrderTotalCalculationService : IOrderTotalCalculationService
    {
        public virtual decimal? GetShoppingCartTotal(IList<ShoppingCartItem> cart,
            IList<ShippingCart> shippingCarts,
            bool ignoreRewardPonts = false, bool usePaymentMethodAdditionalFee = true)
        {
            decimal discountAmount;
            Discount appliedDiscount;
            int redeemedRewardPoints;
            decimal redeemedRewardPointsAmount;
            List<AppliedGiftCard> appliedGiftCards;
            return GetShoppingCartTotal(cart,
                shippingCarts,
                out discountAmount,
                out appliedDiscount,
                out appliedGiftCards,
                out redeemedRewardPoints,
                out redeemedRewardPointsAmount,
                ignoreRewardPonts,
                usePaymentMethodAdditionalFee);
        }

        public virtual decimal? GetShoppingCartTotal(IList<ShoppingCartItem> cart,
            IList<ShippingCart> shippingCarts,
            out decimal discountAmount, out Discount appliedDiscount,
            out List<AppliedGiftCard> appliedGiftCards,
            out int redeemedRewardPoints, out decimal redeemedRewardPointsAmount,
            bool ignoreRewardPonts = false, bool usePaymentMethodAdditionalFee = true)
        {
            redeemedRewardPoints = 0;
            redeemedRewardPointsAmount = decimal.Zero;

            var customer = cart.GetCustomer();
            string paymentMethodSystemName = "";
            if (customer != null)
            {
                paymentMethodSystemName = customer.GetAttribute<string>(
                    SystemCustomerAttributeNames.SelectedPaymentMethod,
                    _genericAttributeService,
                    _storeContext.CurrentStore.Id);
            }


            //subtotal without tax
            decimal orderSubTotalDiscountAmount;
            Discount orderSubTotalAppliedDiscount;
            decimal subTotalWithoutDiscountBase;
            decimal subTotalWithDiscountBase;
            GetShoppingCartSubTotal(cart, false,
                out orderSubTotalDiscountAmount, out orderSubTotalAppliedDiscount,
                out subTotalWithoutDiscountBase, out subTotalWithDiscountBase);
            //subtotal with discount
            decimal subtotalBase = subTotalWithDiscountBase;



            //shipping without tax
            decimal? shoppingCartShipping = GetShoppingCartShippingTotal(cart, false);



            //payment method additional fee without tax
            decimal paymentMethodAdditionalFeeWithoutTax = decimal.Zero;
            if (usePaymentMethodAdditionalFee && !String.IsNullOrEmpty(paymentMethodSystemName))
            {
                decimal paymentMethodAdditionalFee = _paymentService.GetAdditionalHandlingFee(cart,
                    shippingCarts,
                    paymentMethodSystemName);
                paymentMethodAdditionalFeeWithoutTax =
                    _taxService.GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee,
                        false, customer);
            }




            //tax
            decimal shoppingCartTax = GetTaxTotal(cart, usePaymentMethodAdditionalFee);




            //order total
            decimal resultTemp = decimal.Zero;
            resultTemp += subtotalBase;
            if (shoppingCartShipping.HasValue)
            {
                resultTemp += shoppingCartShipping.Value;
            }
            resultTemp += paymentMethodAdditionalFeeWithoutTax;
            resultTemp += shoppingCartTax;
            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                resultTemp = RoundingHelper.RoundPrice(resultTemp);

            #region Order total discount

            discountAmount = GetOrderTotalDiscount(customer, resultTemp, out appliedDiscount);

            //sub totals with discount        
            if (resultTemp < discountAmount)
                discountAmount = resultTemp;

            //reduce subtotal
            resultTemp -= discountAmount;

            if (resultTemp < decimal.Zero)
                resultTemp = decimal.Zero;
            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                resultTemp = RoundingHelper.RoundPrice(resultTemp);

            #endregion

            #region Applied gift cards

            //let's apply gift cards now (gift cards that can be used)
            appliedGiftCards = new List<AppliedGiftCard>();
            if (!cart.IsRecurring())
            {
                //we don't apply gift cards for recurring products
                var giftCards = _giftCardService.GetActiveGiftCardsAppliedByCustomer(customer);
                if (giftCards != null)
                    foreach (var gc in giftCards)
                        if (resultTemp > decimal.Zero)
                        {
                            decimal remainingAmount = gc.GetGiftCardRemainingAmount();
                            decimal amountCanBeUsed = decimal.Zero;
                            if (resultTemp > remainingAmount)
                                amountCanBeUsed = remainingAmount;
                            else
                                amountCanBeUsed = resultTemp;

                            //reduce subtotal
                            resultTemp -= amountCanBeUsed;

                            var appliedGiftCard = new AppliedGiftCard();
                            appliedGiftCard.GiftCard = gc;
                            appliedGiftCard.AmountCanBeUsed = amountCanBeUsed;
                            appliedGiftCards.Add(appliedGiftCard);
                        }
            }

            #endregion

            if (resultTemp < decimal.Zero)
                resultTemp = decimal.Zero;
            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                resultTemp = RoundingHelper.RoundPrice(resultTemp);

            if (!shoppingCartShipping.HasValue)
            {
                //we have errors
                return null;
            }

            decimal orderTotal = resultTemp;

            #region Reward points

            if (_rewardPointsSettings.Enabled &&
                !ignoreRewardPonts &&
                customer.GetAttribute<bool>(SystemCustomerAttributeNames.UseRewardPointsDuringCheckout,
                    _genericAttributeService, _storeContext.CurrentStore.Id))
            {
                int rewardPointsBalance = customer.GetRewardPointsBalance();
                if (CheckMinimumRewardPointsToUseRequirement(rewardPointsBalance))
                {
                    decimal rewardPointsBalanceAmount = ConvertRewardPointsToAmount(rewardPointsBalance);
                    if (orderTotal > decimal.Zero)
                    {
                        if (orderTotal > rewardPointsBalanceAmount)
                        {
                            redeemedRewardPoints = rewardPointsBalance;
                            redeemedRewardPointsAmount = rewardPointsBalanceAmount;
                        }
                        else
                        {
                            redeemedRewardPointsAmount = orderTotal;
                            redeemedRewardPoints = ConvertAmountToRewardPoints(redeemedRewardPointsAmount);
                        }
                    }
                }
            }

            #endregion

            orderTotal = orderTotal - redeemedRewardPointsAmount;
            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                orderTotal = RoundingHelper.RoundPrice(orderTotal);

            foreach (var shippingCart in shippingCarts)
            {
                orderTotal += shippingCart.ShippingFee;
            }

            return orderTotal;
        }
    }
}
