using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Models.Orders;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Admin.Controllers
{
    public partial class OrderController : BaseAdminController
    {
        [HttpPost]
        public ActionResult ShipmentsByOrderShipping(int orderShippingId, DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var orderShipping = _orderShippingService.GetOrderShippingById(orderShippingId);
            if (orderShipping == null)
                throw new ArgumentException("No order shipping found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToOrder(orderShipping.Order))
                return Content("");

            //shipments
            var shipmentModels = new List<ShipmentModel>();
            //shipments
            var shipments =
                orderShipping.Shipments.Where(s => _workContext.CurrentVendor == null || HasAccessToShipment(s))
                    .OrderBy(x => x.CreatedOnUtc)
                    .ToList();

            foreach (var shipment in shipments)
                shipmentModels.Add(PrepareShipmentModel(shipment, false));

            var gridModel = new DataSourceResult
            {
                Data = shipmentModels,
                Total = shipmentModels.Count
            };

            return Json(gridModel);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("save-shipping-method-of-order-shipping")]
        public ActionResult EditShippingMethodOfOrderShipping(OrderModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var orderShipping = _orderShippingService.GetOrderShippingById(model.CurrentOrderShippingIdEdit);
            if (orderShipping == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null)
                return RedirectToAction("Edit", "Order", new { id = model.Id });

            //TODO: update Shipping method
            orderShipping.ShippingMethod = model.CurrentShippingMethodEdit;
            _orderShippingService.UpdateOrderShipping(orderShipping);

            var order = _orderService.GetOrderById(model.Id);
            //add a note
            order.OrderNotes.Add(new OrderNote
            {
                Note = "Shipping method has been edited",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            _orderService.UpdateOrder(order);

            PrepareOrderDetailsModel(model, order);

            //selected tab
            SaveSelectedTabIndex(persistForTheNextRequest: false);

            return View(model);
        }

        public ActionResult AddShipmentToOrderShipping(int orderShippingId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var orderShipping = _orderShippingService.GetOrderShippingById(orderShippingId);
            if (orderShipping == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToOrder(orderShipping.Order))
                return RedirectToAction("List");

            var model = new ShipmentModel
            {
                OrderId = orderShipping.OrderId,
                OrderShippingId = orderShipping.Id
            };

            //measures
            var baseWeight = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);
            var baseWeightIn = baseWeight != null ? baseWeight.Name : "";
            var baseDimension = _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);
            var baseDimensionIn = baseDimension != null ? baseDimension.Name : "";

            //var orderShippingItems = _orderShippingService.GetOrderShippingItemsByOrderShippingId(orderShippingId);
            var orderShippingItems = orderShipping.OrderShippingItems;
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                orderShippingItems = orderShippingItems.Where(HasAccessToOrderShippingItem).ToList();
            }

            foreach (var orderShippingItem in orderShippingItems)
            {
                //we can ship only shippable products
                if (!orderShippingItem.OrderItem.Product.IsShipEnabled)
                    continue;

                //quantities
                var qtyInThisShipment = 0;
                var maxQtyToAdd = orderShippingItem.GetTotalNumberOfItemsCanBeAddedToShipment();
                var qtyOrdered = orderShippingItem.Quantity;
                var qtyInAllShipments = orderShippingItem.GetTotalNumberOfItemsInAllShipment();

                //ensure that this product can be added to a shipment
                if (maxQtyToAdd <= 0)
                    continue;

                var shipmentItemModel = new ShipmentModel.ShipmentItemModel
                {
                    OrderItemId = orderShippingItem.OrderItem.Id,
                    OrderShippingItemId = orderShippingItem.Id,
                    ProductId = orderShippingItem.OrderItem.ProductId,
                    ProductName = orderShippingItem.OrderItem.Product.Name,
                    Sku = orderShippingItem.OrderItem.Product.FormatSku(orderShippingItem.OrderItem.AttributesXml, _productAttributeParser),
                    AttributeInfo = orderShippingItem.OrderItem.AttributeDescription,
                    ShipSeparately = orderShippingItem.OrderItem.Product.ShipSeparately,
                    ItemWeight = orderShippingItem.OrderItem.ItemWeight.HasValue ? string.Format("{0:F2} [{1}]", orderShippingItem.OrderItem.ItemWeight, baseWeightIn) : "",
                    ItemDimensions = string.Format("{0:F2} x {1:F2} x {2:F2} [{3}]", orderShippingItem.OrderItem.Product.Length, orderShippingItem.OrderItem.Product.Width, orderShippingItem.OrderItem.Product.Height, baseDimensionIn),
                    QuantityOrdered = qtyOrdered,
                    QuantityInThisShipment = qtyInThisShipment,
                    QuantityInAllShipments = qtyInAllShipments,
                    QuantityToAdd = maxQtyToAdd,
                };
                //rental info
                if (orderShippingItem.OrderItem.Product.IsRental)
                {
                    var rentalStartDate = orderShippingItem.OrderItem.RentalStartDateUtc.HasValue ? orderShippingItem.OrderItem.Product.FormatRentalDate(orderShippingItem.OrderItem.RentalStartDateUtc.Value) : "";
                    var rentalEndDate = orderShippingItem.OrderItem.RentalEndDateUtc.HasValue ? orderShippingItem.OrderItem.Product.FormatRentalDate(orderShippingItem.OrderItem.RentalEndDateUtc.Value) : "";
                    shipmentItemModel.RentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"),
                        rentalStartDate, rentalEndDate);
                }

                if (orderShippingItem.OrderItem.Product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                    orderShippingItem.OrderItem.Product.UseMultipleWarehouses)
                {
                    //multiple warehouses supported
                    shipmentItemModel.AllowToChooseWarehouse = true;
                    foreach (var pwi in orderShippingItem.OrderItem.Product.ProductWarehouseInventory
                        .OrderBy(w => w.WarehouseId).ToList())
                    {
                        var warehouse = pwi.Warehouse;
                        if (warehouse != null)
                        {
                            shipmentItemModel.AvailableWarehouses.Add(new ShipmentModel.ShipmentItemModel.WarehouseInfo
                            {
                                WarehouseId = warehouse.Id,
                                WarehouseName = warehouse.Name,
                                StockQuantity = pwi.StockQuantity,
                                ReservedQuantity = pwi.ReservedQuantity,
                                PlannedQuantity = _shipmentService.GetQuantityInShipments(orderShippingItem.OrderItem.Product, warehouse.Id, true, true)
                            });
                        }
                    }
                }
                else
                {
                    //multiple warehouses are not supported
                    var warehouse = _shippingService.GetWarehouseById(orderShippingItem.OrderItem.Product.WarehouseId);
                    if (warehouse != null)
                    {
                        shipmentItemModel.AvailableWarehouses.Add(new ShipmentModel.ShipmentItemModel.WarehouseInfo
                        {
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            StockQuantity = orderShippingItem.OrderItem.Product.StockQuantity
                        });
                    }
                }

                model.Items.Add(shipmentItemModel);
            }

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        public ActionResult AddShipmentToOrderShipping(int orderShippingId, FormCollection form, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var orderShipping = _orderShippingService.GetOrderShippingById(orderShippingId);
            if (orderShipping == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToOrder(orderShipping.Order))
                return RedirectToAction("List");

            //var orderShippingItems = _orderShippingService.GetOrderShippingItemsByOrderShippingId(orderShippingId);
            var orderShippingItems = orderShipping.OrderShippingItems;
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                orderShippingItems = orderShippingItems.Where(HasAccessToOrderShippingItem).ToList();
            }

            Shipment shipment = null;
            decimal? totalWeight = null;
            foreach (var orderShippingItem in orderShippingItems)
            {
                //is shippable
                if (!orderShippingItem.OrderItem.Product.IsShipEnabled)
                    continue;

                //ensure that this product can be shipped (have at least one item to ship)
                var maxQtyToAdd = orderShippingItem.GetTotalNumberOfItemsCanBeAddedToShipment();
                if (maxQtyToAdd <= 0)
                    continue;

                int qtyToAdd = 0; //parse quantity
                foreach (string formKey in form.AllKeys)
                    if (formKey.Equals(string.Format("qtyToAdd{0}", orderShippingItem.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        int.TryParse(form[formKey], out qtyToAdd);
                        break;
                    }

                int warehouseId = 0;
                if (orderShippingItem.OrderItem.Product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                    orderShippingItem.OrderItem.Product.UseMultipleWarehouses)
                {
                    //multiple warehouses supported
                    //warehouse is chosen by a store owner
                    foreach (string formKey in form.AllKeys)
                        if (formKey.Equals(string.Format("warehouse_{0}", orderShippingItem.Id), StringComparison.InvariantCultureIgnoreCase))
                        {
                            int.TryParse(form[formKey], out warehouseId);
                            break;
                        }
                }
                else
                {
                    //multiple warehouses are not supported
                    warehouseId = orderShippingItem.OrderItem.Product.WarehouseId;
                }

                foreach (string formKey in form.AllKeys)
                    if (formKey.Equals(string.Format("qtyToAdd{0}", orderShippingItem.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        int.TryParse(form[formKey], out qtyToAdd);
                        break;
                    }

                //validate quantity
                if (qtyToAdd <= 0)
                    continue;
                if (qtyToAdd > maxQtyToAdd)
                    qtyToAdd = maxQtyToAdd;

                //ok. we have at least one item. let's create a shipment (if it does not exist)

                var orderItemTotalWeight = orderShippingItem.OrderItem.ItemWeight.HasValue ? orderShippingItem.OrderItem.ItemWeight * qtyToAdd : null;
                if (orderItemTotalWeight.HasValue)
                {
                    if (!totalWeight.HasValue)
                        totalWeight = 0;
                    totalWeight += orderItemTotalWeight.Value;
                }
                if (shipment == null)
                {
                    var trackingNumber = form["TrackingNumber"];
                    var adminComment = form["AdminComment"];
                    shipment = new Shipment
                    {
                        //TODO: add OrderShipping
                        OrderShippingId = orderShipping.Id,
                        TrackingNumber = trackingNumber,
                        TotalWeight = null,
                        ShippedDateUtc = null,
                        DeliveryDateUtc = null,
                        AdminComment = adminComment,
                        CreatedOnUtc = DateTime.UtcNow,
                    };
                }
                //create a shipment item
                var shipmentItem = new ShipmentItem
                {
                    OrderItemId = orderShippingItem.OrderItem.Id,
                    OrderShippingItemId = orderShippingItem.Id,
                    Quantity = qtyToAdd,
                    WarehouseId = warehouseId
                };
                shipment.ShipmentItems.Add(shipmentItem);
            }

            //if we have at least one item in the shipment, then save it
            if (shipment != null && shipment.ShipmentItems.Count > 0)
            {
                shipment.TotalWeight = totalWeight;
                _shipmentService.InsertShipment(shipment);

                //add a note
                orderShipping.Order.OrderNotes.Add(new OrderNote
                {
                    Note = "A shipment has been added",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                _orderService.UpdateOrder(orderShipping.Order);

                SuccessNotification(_localizationService.GetResource("Admin.Orders.Shipments.Added"));
                return continueEditing
                           ? RedirectToAction("ShipmentDetails", new { id = shipment.Id })
                           : RedirectToAction("Edit", new { id = orderShipping.OrderId });
            }

            ErrorNotification(_localizationService.GetResource("Admin.Orders.Shipments.NoProductsSelected"));
            return RedirectToAction("AddShipmentToOrderShipping", new { orderShippingId = orderShippingId });
        }

        public ActionResult ShipmentDetails(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var shipment = _shipmentService.GetShipmentById(id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToShipment(shipment))
                return RedirectToAction("List");

            var model = PrepareShipmentModel(shipment, true, true);
            return View(model);
        }

        [HttpPost]
        public ActionResult DeleteShipment(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var shipment = _shipmentService.GetShipmentById(id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToShipment(shipment))
                return RedirectToAction("List");

            foreach (var shipmentItem in shipment.ShipmentItems)
            {
                var orderItem = _orderService.GetOrderItemById(shipmentItem.OrderItemId);
                if (orderItem == null)
                    continue;

                _productService.ReverseBookedInventory(orderItem.Product, shipmentItem);
            }

            var orderId = shipment.OrderShipping.OrderId;
            _shipmentService.DeleteShipment(shipment);

            var order = _orderService.GetOrderById(orderId);
            //add a note
            order.OrderNotes.Add(new OrderNote
            {
                Note = "A shipment has been deleted",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            _orderService.UpdateOrder(order);

            SuccessNotification(_localizationService.GetResource("Admin.Orders.Shipments.Deleted"));
            return RedirectToAction("Edit", new { id = orderId });
        }

        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("settrackingnumber")]
        public ActionResult SetTrackingNumber(ShipmentModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var shipment = _shipmentService.GetShipmentById(model.Id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToShipment(shipment))
                return RedirectToAction("List");

            shipment.TrackingNumber = model.TrackingNumber;
            _shipmentService.UpdateShipment(shipment);

            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }

        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("setadmincomment")]
        public ActionResult SetShipmentAdminComment(ShipmentModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var shipment = _shipmentService.GetShipmentById(model.Id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToShipment(shipment))
                return RedirectToAction("List");

            shipment.AdminComment = model.AdminComment;
            _shipmentService.UpdateShipment(shipment);

            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }

        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("setasshipped")]
        public ActionResult SetAsShipped(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var shipment = _shipmentService.GetShipmentById(id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToShipment(shipment))
                return RedirectToAction("List");

            try
            {
                _orderProcessingService.Ship(shipment, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                ErrorNotification(exc, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }

        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("saveshippeddate")]
        public ActionResult EditShippedDate(ShipmentModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var shipment = _shipmentService.GetShipmentById(model.Id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToShipment(shipment))
                return RedirectToAction("List");

            try
            {
                if (!model.ShippedDateUtc.HasValue)
                {
                    throw new Exception("Enter shipped date");
                }
                shipment.ShippedDateUtc = model.ShippedDateUtc;
                _shipmentService.UpdateShipment(shipment);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                ErrorNotification(exc, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }

        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("setasdelivered")]
        public ActionResult SetAsDelivered(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var shipment = _shipmentService.GetShipmentById(id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToShipment(shipment))
                return RedirectToAction("List");

            try
            {
                _orderProcessingService.Deliver(shipment, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                ErrorNotification(exc, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }


        [HttpPost, ActionName("ShipmentDetails")]
        [FormValueRequired("savedeliverydate")]
        public ActionResult EditDeliveryDate(ShipmentModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageOrders))
                return AccessDeniedView();

            var shipment = _shipmentService.GetShipmentById(model.Id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !HasAccessToShipment(shipment))
                return RedirectToAction("List");

            try
            {
                if (!model.DeliveryDateUtc.HasValue)
                {
                    throw new Exception("Enter delivery date");
                }
                shipment.DeliveryDateUtc = model.DeliveryDateUtc;
                _shipmentService.UpdateShipment(shipment);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                ErrorNotification(exc, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }

        [NonAction]
        protected virtual bool HasAccessToOrderShippingItem(OrderShippingItem orderShippingItem)
        {
            if (orderShippingItem == null)
                throw new ArgumentNullException("orderShippingItem");

            if (_workContext.CurrentVendor == null)
                //not a vendor; has access
                return true;

            var vendorId = _workContext.CurrentVendor.Id;
            return orderShippingItem.OrderItem.Product.VendorId == vendorId;
        }

        [NonAction]
        protected virtual ShipmentModel PrepareShipmentModel(Shipment shipment, bool prepareProducts, bool prepareShipmentEvent = false)
        {
            //measures
            var baseWeight = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);
            var baseWeightIn = baseWeight != null ? baseWeight.Name : "";
            var baseDimension = _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);
            var baseDimensionIn = baseDimension != null ? baseDimension.Name : "";

            var model = new ShipmentModel
            {
                Id = shipment.Id,
                OrderId = shipment.OrderShipping.OrderId,
                OrderShippingId = shipment.OrderShippingId,
                TrackingNumber = shipment.TrackingNumber,
                TotalWeight = shipment.TotalWeight.HasValue ? string.Format("{0:F2} [{1}]", shipment.TotalWeight, baseWeightIn) : "",
                ShippedDate = shipment.ShippedDateUtc.HasValue ? _dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc).ToString() : _localizationService.GetResource("Admin.Orders.Shipments.ShippedDate.NotYet"),
                ShippedDateUtc = shipment.ShippedDateUtc,
                CanShip = !shipment.ShippedDateUtc.HasValue,
                DeliveryDate = shipment.DeliveryDateUtc.HasValue ? _dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc).ToString() : _localizationService.GetResource("Admin.Orders.Shipments.DeliveryDate.NotYet"),
                DeliveryDateUtc = shipment.DeliveryDateUtc,
                CanDeliver = shipment.ShippedDateUtc.HasValue && !shipment.DeliveryDateUtc.HasValue,
                AdminComment = shipment.AdminComment,
            };

            if (prepareProducts)
            {
                foreach (var shipmentItem in shipment.ShipmentItems)
                {
                    var orderItem = _orderService.GetOrderItemById(shipmentItem.OrderItemId);
                    if (orderItem == null)
                        continue;

                    //quantities
                    var qtyInThisShipment = shipmentItem.Quantity;
                    var maxQtyToAdd = orderItem.GetTotalNumberOfItemsCanBeAddedToShipment();
                    var qtyOrdered = orderItem.Quantity;
                    var qtyInAllShipments = orderItem.GetTotalNumberOfItemsInAllShipment();

                    var warehouse = _shippingService.GetWarehouseById(shipmentItem.WarehouseId);
                    var shipmentItemModel = new ShipmentModel.ShipmentItemModel
                    {
                        Id = shipmentItem.Id,
                        OrderItemId = orderItem.Id,
                        ProductId = orderItem.ProductId,
                        ProductName = orderItem.Product.Name,
                        Sku = orderItem.Product.FormatSku(orderItem.AttributesXml, _productAttributeParser),
                        AttributeInfo = orderItem.AttributeDescription,
                        ShippedFromWarehouse = warehouse != null ? warehouse.Name : null,
                        ShipSeparately = orderItem.Product.ShipSeparately,
                        ItemWeight = orderItem.ItemWeight.HasValue ? string.Format("{0:F2} [{1}]", orderItem.ItemWeight, baseWeightIn) : "",
                        ItemDimensions = string.Format("{0:F2} x {1:F2} x {2:F2} [{3}]", orderItem.Product.Length, orderItem.Product.Width, orderItem.Product.Height, baseDimensionIn),
                        QuantityOrdered = qtyOrdered,
                        QuantityInThisShipment = qtyInThisShipment,
                        QuantityInAllShipments = qtyInAllShipments,
                        QuantityToAdd = maxQtyToAdd,
                    };
                    //rental info
                    if (orderItem.Product.IsRental)
                    {
                        var rentalStartDate = orderItem.RentalStartDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalStartDateUtc.Value) : "";
                        var rentalEndDate = orderItem.RentalEndDateUtc.HasValue ? orderItem.Product.FormatRentalDate(orderItem.RentalEndDateUtc.Value) : "";
                        shipmentItemModel.RentalInfo = string.Format(_localizationService.GetResource("Order.Rental.FormattedDate"),
                            rentalStartDate, rentalEndDate);
                    }

                    model.Items.Add(shipmentItemModel);
                }
            }

            if (prepareShipmentEvent && !String.IsNullOrEmpty(shipment.TrackingNumber))
            {
                var order = shipment.OrderShipping != null ? shipment.OrderShipping.Order : null;
                var srcm = _shippingService.LoadShippingRateComputationMethodBySystemName(order.ShippingRateComputationMethodSystemName);
                if (srcm != null &&
                    srcm.PluginDescriptor.Installed &&
                    srcm.IsShippingRateComputationMethodActive(_shippingSettings))
                {
                    var shipmentTracker = srcm.ShipmentTracker;
                    if (shipmentTracker != null)
                    {
                        model.TrackingNumberUrl = shipmentTracker.GetUrl(shipment.TrackingNumber);
                        if (_shippingSettings.DisplayShipmentEventsToStoreOwner)
                        {
                            var shipmentEvents = shipmentTracker.GetShipmentEvents(shipment.TrackingNumber);
                            if (shipmentEvents != null)
                            {
                                foreach (var shipmentEvent in shipmentEvents)
                                {
                                    var shipmentStatusEventModel = new ShipmentModel.ShipmentStatusEventModel();
                                    var shipmentEventCountry = _countryService.GetCountryByTwoLetterIsoCode(shipmentEvent.CountryCode);
                                    shipmentStatusEventModel.Country = shipmentEventCountry != null
                                        ? shipmentEventCountry.GetLocalized(x => x.Name)
                                        : shipmentEvent.CountryCode;
                                    shipmentStatusEventModel.Date = shipmentEvent.Date;
                                    shipmentStatusEventModel.EventName = shipmentEvent.EventName;
                                    shipmentStatusEventModel.Location = shipmentEvent.Location;
                                    model.ShipmentStatusEvents.Add(shipmentStatusEventModel);
                                }
                            }
                        }
                    }
                }
            }

            return model;
        }
    }
}