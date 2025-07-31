using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Infrastructure.Mocks
{
    public interface IEmailService
    {
        Task SendEmailAsync(Guid orderId,string email);
    }
}
