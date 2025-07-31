using Microsoft.Extensions.Logging;
using OrderService.Domain;
using OrderService.Domain.Events;
using OrderService.Persistence.DbContexts;

namespace OrderService.Persistence.Repositories
{

    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _orderDbContext;
        private readonly List<OrderEntity> _orders;
        private OrderEntity _order;
        private readonly ILogger _logger;

        public OrderRepository(ILogger<OrderRepository> logger)
        {
            _logger = logger;
            _orders = new List<OrderEntity>
            {
                new OrderEntity("Summer Campaign - Shoes", 200000),
                new OrderEntity("Lookbook April - Accessories", 150000),
                new OrderEntity("Sale Event - Bags Collection", 175000),
                new OrderEntity("Fall Campaign - Jackets", 250000),
                new OrderEntity("Homepage Banner - Model A", 180000),
                new OrderEntity("New Arrivals - Sportswear", 220000),
                new OrderEntity("Flash Sale - Sunglasses", 160000),
                new OrderEntity("Studio Test - Product Line B", 210000),
                new OrderEntity("Reorder - Classic Set", 195000),
                new OrderEntity("Campaign Teaser - Autumn", 230000),
            };
        }

        public void AddOutboxEvent(IReadOnlyCollection<OrderEvent> orderEvent)
        {
            _logger.LogInformation($"Add {orderEvent.Count} events to outbox!");
        }

        public async Task<OrderEntity> GetByIdAsync(Guid id)
        {
            var order = _orders.FirstOrDefault(x => x.Id.Equals(id));
            _order = order;
            return order;
        }

        /// <summary>
        /// Mocking for update 
        /// </summary>
        /// <returns></returns>
        public Task<bool> SaveChangesAsync()
        {
            _logger.LogInformation($"Save order {_order.Name}!");
            return Task.FromResult(true);
        }

        public async Task<IEnumerable<OrderEntity>> QueryAsync(string name, int page, int pageSize)
        {
            var query = _orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(o => o.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }

            var total = query.Count();

            var paged = query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            return paged;
        }

        public async Task<IEnumerable<OrderEntity>> GetOrdersStuckAsync()
        {
            return _orders.AsQueryable().Where(x => x.Status != OrderStatus.Completed);
        }
    }
}
