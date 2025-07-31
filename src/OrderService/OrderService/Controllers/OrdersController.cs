using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Dtos.Checkout;
using OrderService.Application.Queries;
using OrderService.Application.Queries.Dtos;

namespace OrderService.Controllers
{
    [Route("api/v1/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderCommand<CheckoutDtoCommand, CheckoutDtoReponse> _orderCommand;
        private readonly IOrderQueryService _orderQueryService;
        public OrdersController(IOrderCommand<CheckoutDtoCommand, CheckoutDtoReponse> orderCommand, IOrderQueryService orderQueryService)
        {
            _orderCommand = orderCommand;
            _orderQueryService = orderQueryService;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckoutAsync([FromBody]CheckoutDtoCommand checkoutDtoCommand)
        {
            var commandResult = await _orderCommand.ExecuteAsync(checkoutDtoCommand);
            return Ok(commandResult);
        }

        [HttpGet]
        public async Task<IActionResult> QueryByNameAsync([FromQuery] OrderQueryByNameDto orderQueryByNameDto)
        {
            var queryResult = await _orderQueryService.QueryAsync(orderQueryByNameDto);
            return Ok(queryResult);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult>GetByIdAsync(Guid id)
        {
            return Ok(await _orderQueryService.GetByIdAsync(id));
        }
    }
}
