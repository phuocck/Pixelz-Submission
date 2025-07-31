using OrderService.Application.Queries.Base;

namespace OrderService.Application.Queries.Dtos
{
    public class OrderQueryDtoResult : PagedResponse<OrderDto>
    {
        public OrderQueryDtoResult(IEnumerable<OrderDto> items, int page, int pageSize) : base(items, page, pageSize)
        {
        }
    }
}
