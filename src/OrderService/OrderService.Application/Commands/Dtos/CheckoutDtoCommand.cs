using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Dtos.Checkout
{
    public class CheckoutDtoCommand
    {
        public Guid OrderId { get; set; }
    }
}
