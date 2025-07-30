using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSerivce.Domain.Events
{
    public abstract class OrderEvent
    {
    }
    public class OrderPaidEvent : OrderEvent
    {
        public Guid OrderId { get; }

        public OrderPaidEvent(Guid orderId)
        {
            OrderId = orderId;
        }
    }
}
