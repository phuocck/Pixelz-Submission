using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Mocks
{
    public class ProductionClient : IProductionClient
    {
        private readonly ILogger _logger;
        public Task<bool> PushAsync(Guid orderId)
        {
            _logger.LogInformation($"Pushed Order {orderId} to Production");
            return Task.FromResult(true);
        }
    }
}
