using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrderService.Application.Queries.Dtos;

namespace OrderService.Application.Queries
{
    public interface IOrderQueryService
    {
        Task<OrderQueryDtoResult> QueryAsync(OrderQueryByNameDto orderQueryByNameDtocs);
        Task<OrderDto> GetByIdAsync(Guid id);
    }
}
