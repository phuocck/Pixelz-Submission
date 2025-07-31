using OrderService.Application.Dtos.Checkout;
using OrderService.Application.Exceptions;
using OrderService.Domain;
using OrderSevice.Infrastructure;
using OrderSevice.Infrastructure.Mocks;
using System.Diagnostics;

namespace OrderService.Application.Commands
{
    public class CheckoutCommandHandler : IOrderCommand<CheckoutDtoCommand, CheckoutDtoReponse>
    {
        private static readonly ActivitySource ActivitySource = new("OrderService.Checkout");

        private readonly IOrderRepository _orderRepository;
        private readonly IEmailService _emailService;
        private readonly IProductionClient _productionClient;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IInvoiceService _invoiceService;

        public CheckoutCommandHandler(
            IOrderRepository orderRepository,
            IEmailService emailService,
            IProductionClient productionClient,
            IPaymentGateway paymentGateway,
            IInvoiceService invoiceService)
        {
            _orderRepository = orderRepository;
            _emailService = emailService;
            _productionClient = productionClient;
            _paymentGateway = paymentGateway;
            _invoiceService = invoiceService;
        }

        public async Task<CheckoutDtoReponse> ExecuteAsync(CheckoutDtoCommand command)
        {
            using var activity = ActivitySource.StartActivity("CheckoutOrder", ActivityKind.Server);
            activity?.SetTag("order.id", command.OrderId);

            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            if (order == null) throw new NotFoundException("Order not found");

            var userId = GetUserId();
            if (order.UserId != userId) throw new UnauthorizedAccessException("Forbidden!");

            if (order.Status != OrderStatus.Created) throw new InvalidOrderStateException("Order is not in a valid state for checkout");

            order.MarkPaymentPending();

            using (var paymentActivity = ActivitySource.StartActivity("Payment"))
            {
                var paymentResult = await RetryPolicy.RetryAsync(() => _paymentGateway.ChargeAsync(order.Id, order.TotalAmount));
                paymentActivity?.SetTag("payment.success", paymentResult);

                if (!paymentResult)
                {
                    order.MarkPaymentFailed();
                    await _orderRepository.SaveChangesAsync();
                    throw new PaymentFailedException("Payment was declined by provider");
                }
            }

            order.MarkPaid();

            //TODO => commit order and commit outbox with the same transaction
            _orderRepository.AddOutboxEvent(order.Events);
            await _orderRepository.SaveChangesAsync();

            await ExecutePostPaymentStepsWithRetry(order);

            return new CheckoutDtoReponse { Success = true };
        }

        /// <summary>
        /// TODO => if use outbox pattern, this method will not handler anything, consumer will handler it
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private async Task ExecutePostPaymentStepsWithRetry(OrderEntity order)
        {
            order.MarkProcessing();
            await _orderRepository.SaveChangesAsync();

            using var activity = ActivitySource.StartActivity("PostPaymentFlow");

            try
            {
                await RetryPolicy.RetryAsync(
                    () => _emailService.SendEmailAsync(order.Id, GetUserEmail()),
                    maxRetries: 3,
                    delay: TimeSpan.FromMilliseconds(500),
                    operationName: "SendEmail"
                );

                await RetryPolicy.RetryAsync(
                    () => _invoiceService.CreateInvoiceAsync(order.Id, order.TotalAmount),
                    maxRetries: 3,
                    delay: TimeSpan.FromMilliseconds(500),
                    operationName: "CreateInvoice"
                );

                await RetryPolicy.RetryAsync(
                    () => _productionClient.PushAsync(order.Id),
                    maxRetries: 2,
                    delay: TimeSpan.FromMilliseconds(500),
                    operationName: "PushToProduction"
                );

                order.MarkCompleted();
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] One or more post-payment steps failed: {ex.Message}");
                order.MarkProcessingFailed();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddEvent(new ActivityEvent("Post-processing failed"));
            }

            await _orderRepository.SaveChangesAsync();
        }

        private string GetUserEmail() => "user-mock@gmail.com";

        private Guid GetUserId() => Guid.Parse("0d567094-fe45-4d89-a338-5ca07e68a340");
    }
}