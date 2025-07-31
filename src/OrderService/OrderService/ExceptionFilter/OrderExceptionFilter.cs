using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OrderService.Application.Exceptions;
using System.Net;

namespace OrderService.Api.ExceptionFilter
{
    public class OrderExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<OrderExceptionFilter> _logger;
        private readonly IHostEnvironment _env;
        public OrderExceptionFilter(ILogger<OrderExceptionFilter> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }
        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception.Message, context.Exception);
            var errorMessage = _env.IsProduction() ? "Exception" : context.Exception.Message;
            var (statusCode, message) = context.Exception switch
            {
                ArgumentNullException => (HttpStatusCode.BadRequest, "Missing required argument"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
                InvalidOperationException => (HttpStatusCode.Conflict, "Invalid operation"),
                InvalidOrderStateException => (HttpStatusCode.UnprocessableEntity, "Invalid Order State"),
                NotFoundException => (HttpStatusCode.NotFound, "Not found"),
                PaymentFailedException => (HttpStatusCode.UnprocessableEntity, "Payment Fail"),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };
            var problemDetails = new ProblemDetails
            {
                Title = message,
                Status = (int)statusCode,
                Detail = errorMessage
            };

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = (int)statusCode
            };
            context.ExceptionHandled = true;
        }
    }
}
