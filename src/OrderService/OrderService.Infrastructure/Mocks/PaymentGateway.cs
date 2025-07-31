using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Mocks
{
    public class PaymentGateway : IPaymentGateway
    {
        private readonly ILogger _logger;
        public PaymentGateway(ILogger<PaymentGateway> logger)
        {
            _logger = logger;
        }
        public Task<bool> ChargeAsync(Guid orderId, decimal amount)
        {
            _logger.LogInformation($"Charge Success order {orderId} total {amount}$");
            return Task.FromResult(true);
        }
    }
}
