using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSevice.Infrastructure.Mocks
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ILogger _logger;
        public InvoiceService(ILogger<InvoiceService> logger)
        {
            _logger = logger;
        }
        public Task<bool> CreateInvoiceAsync(Guid orderId, decimal totalAmount)
        {
            _logger.LogInformation($"Created Invoice for order {orderId}");
            return Task.FromResult(true);
        }
    }
}
