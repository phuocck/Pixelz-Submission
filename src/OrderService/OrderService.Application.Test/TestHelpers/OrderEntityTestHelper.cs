using OrderService.Domain;
using System.Reflection;

namespace OrderService.Application.Test.TestHelpers
{
    public static class OrderEntityTestHelper
    {
        public static OrderEntity CreateTestOrder(string name, decimal amount, Guid? id = null, Guid? userId = null)
        {
            var order = new OrderEntity(name, amount);
            
            if (id.HasValue)
            {
                var idProperty = typeof(OrderEntity).GetProperty("Id");
                idProperty?.SetValue(order, id.Value);
            }
            
            if (userId.HasValue)
            {
                var userIdProperty = typeof(OrderEntity).GetProperty("UserId");
                userIdProperty?.SetValue(order, userId.Value);
            }
            
            return order;
        }

        public static OrderEntity CreateTestOrderWithStatus(string name, decimal amount, OrderStatus status, Guid? id = null, Guid? userId = null)
        {
            var order = CreateTestOrder(name, amount, id, userId);
            
            // Use reflection to set the status
            var statusProperty = typeof(OrderEntity).GetProperty("Status");
            statusProperty?.SetValue(order, status);
            
            return order;
        }
    }
} 