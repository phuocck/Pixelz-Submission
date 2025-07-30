using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Commands
{
    public interface IOrderCommand<TIn,TOut>
    {
        Task<TOut> ExecuteAsync(TIn command);
    }
}
