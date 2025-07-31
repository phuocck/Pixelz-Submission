using OrderService.Domain;

namespace OrderService.Application.Queries.Dtos
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public static OrderDto ToDto(OrderEntity orderEntity)
        {
            return new OrderDto
            {
                CreatedAt = orderEntity.CreatedAt,
                Id = orderEntity.Id,
                Name = orderEntity.Name,
                Status = orderEntity.Status,
                TotalAmount = orderEntity.TotalAmount,
                UpdatedAt = orderEntity.UpdatedAt
            };
        }
    }
}
