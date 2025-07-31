using OrderSerivce.Domain;
using OrderService.Application.Queries.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Queries
{
    public class OrderQueryService : IOrderQueryService
    {
        private readonly IOrderRepository _orderRepository;
        public OrderQueryService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<OrderDto> GetByIdAsync(Guid id)
        {
            var entity = await _orderRepository.GetByIdAsync(id);
            return OrderDto.ToDto(entity);
        }

        public async Task<OrderQueryDtoResult> QueryAsync(OrderQueryByNameDto orderQueryByNameDtocs)
        {
            var entites = await _orderRepository.QueryAsync(orderQueryByNameDtocs.Name, orderQueryByNameDtocs.Page, orderQueryByNameDtocs.PageSize);
            var items = entites.Select(e => new OrderDto
            {
                CreatedAt = e.CreatedAt,
                Id = e.Id,
                Name = e.Name,
                Status = e.Status,
                TotalAmount = e.TotalAmount,
                UpdatedAt = e.UpdatedAt
            });
            return new OrderQueryDtoResult(items, orderQueryByNameDtocs.Page, orderQueryByNameDtocs.PageSize);
        }
    }
}
