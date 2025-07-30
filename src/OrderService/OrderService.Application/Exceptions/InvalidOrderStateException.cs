using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Exceptions
{
    public class InvalidOrderStateException:Exception
    {
        public InvalidOrderStateException(string message) : base(message)
        {

        }
    }
}
