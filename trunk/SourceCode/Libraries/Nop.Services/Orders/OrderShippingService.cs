using System;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;

namespace Nop.Services.Orders
{
    public class OrderShippingService : IOrderShippingService
    {
        private IRepository<OrderShipping> _orderShippingRepository;
        private readonly IEventPublisher _eventPublisher;

        public OrderShippingService(
            IRepository<OrderShipping> orderShippingRespository,
            IEventPublisher eventPublisher)
        {
            _orderShippingRepository = orderShippingRespository;
            _eventPublisher = eventPublisher;
        }

        public OrderShipping GetOrderShippingById(int orderShippingId)
        {
            if (orderShippingId == 0) return null;

            return _orderShippingRepository.GetById(orderShippingId);
        }

        public void UpdateOrderShipping(OrderShipping orderShipping)
        {
            if (orderShipping == null)
                throw new ArgumentNullException("orderShipping");

            _orderShippingRepository.Update(orderShipping);

            //event notification
            _eventPublisher.EntityUpdated(orderShipping);
        }
    }
}
