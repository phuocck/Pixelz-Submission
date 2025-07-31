using OrderSerivce.Domain;
using OrderService.Application.Queries.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Queries.Dtos
{
    public class OrderQueryDtoResult : PagedResponse<OrderDto>
    {
        public OrderQueryDtoResult(IEnumerable<OrderDto> items, int page, int pageSize) : base(items, page, pageSize)
        {
        }
    }
}
