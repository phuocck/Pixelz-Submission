using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Mocks
{
    public class EmailService : IEmailService
    {
        private readonly ILogger _logger;
        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(Guid orderId, string email)
        {
            _logger.LogInformation($"Send email to {email} successfully");
            return Task.CompletedTask;
        }
    }
}
