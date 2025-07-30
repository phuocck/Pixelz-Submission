using OrderSerivce.Domain.Events;
using OrderService.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderSerivce.Domain
{
    public interface IOrderRepository
    {
        Task<OrderEntity> GetByIdAsync(Guid id);

        Task<bool> SaveChangeAsync();


        void AddOutboxEvent(IReadOnlyCollection<OrderEvent> orderEvent);
    }
}
