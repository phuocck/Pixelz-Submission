using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSevice.Infrastructure.Mocks
{
    public interface IInvoiceService
    {
        Task<bool> CreateInvoiceAsync(Guid orderId, decimal totalAmount);
    }
}
