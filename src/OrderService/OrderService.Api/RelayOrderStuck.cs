using OrderService.Domain;

namespace OrderService.Api
{
    public class RelayOrderStuck
    {
        private readonly IOrderRepository _orderRepository;
        public async Task ExecuteAsync()
        {
            while (true)
            {
                await Task.Delay(5000);
                Console.WriteLine("Execute task!");

                var ordersStuck =await _orderRepository.GetOrdersStuckAsync();

                if (ordersStuck.Any())
                {
                    Console.WriteLine($"Founded {ordersStuck.Count()} stuck, relay all of it!");
                    //TODO => handler order stuck
                }
            }
        }
    }
}
