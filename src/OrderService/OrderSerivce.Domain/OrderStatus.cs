using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSerivce.Domain
{
    public enum OrderStatus
    {
        Created,
        PaymentPending,
        PaymentFailed,
        Paid,
        Processing,
        ProcessingFailed,
        Completed
    }
}
