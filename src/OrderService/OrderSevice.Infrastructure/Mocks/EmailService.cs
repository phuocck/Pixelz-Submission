using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSevice.Infrastructure.Mocks
{
    public class EmailService : IEmailService
    {
        public Task SendEmailAsync(Guid orderId, string email)
        {
            throw new NotImplementedException();
        }
    }
}
