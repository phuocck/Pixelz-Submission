using OrderService.Domain.Events;

namespace OrderService.Domain
{
    public class OrderEntity
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Decimal TotalAmount { get; private set; }
        public OrderStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public Guid? UserId { get; private set; }

        private readonly List<OrderEvent> _events = new();
        public IReadOnlyCollection<OrderEvent> Events => _events.AsReadOnly();

        private OrderEntity() { } // For EF

        public OrderEntity(string name, Decimal totalAmount)
        {
            if (totalAmount < 0) throw new ArgumentException("total amount must >= 0");
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Status = OrderStatus.Created;
            TotalAmount = totalAmount;
            CreatedAt = DateTime.UtcNow;
            //TODO => fix user
            UserId = Guid.Parse("0d567094-fe45-4d89-a338-5ca07e68a340");
        }

        public void MarkPaymentPending()
        {
            if (Status != OrderStatus.Created)
                throw new InvalidOperationException("Order must be in Created state to start payment.");

            Status = OrderStatus.PaymentPending;
            Touch();
        }

        public void MarkPaymentFailed()
        {
            if (Status != OrderStatus.PaymentPending)
                throw new InvalidOperationException("Order must be in PaymentPending state to fail.");

            Status = OrderStatus.PaymentFailed;
            Touch();
        }

        public void MarkPaid()
        {
            if (Status != OrderStatus.PaymentPending)
                throw new InvalidOperationException("Order must be in PaymentPending state to succeed.");

            Status = OrderStatus.Paid;
            AddEvent(new OrderPaidEvent(Id));
            Touch();
        }

        public void MarkProcessing()
        {
            if (Status != OrderStatus.Paid)
                throw new InvalidOperationException("Order must be Paid to begin processing.");

            Status = OrderStatus.Processing;
            Touch();
        }

        public void MarkProcessingFailed()
        {
            if (Status != OrderStatus.Processing)
                throw new InvalidOperationException("Only processing orders can fail.");

            Status = OrderStatus.ProcessingFailed;
            Touch();
        }

        public void MarkCompleted()
        {
            if (Status != OrderStatus.Processing)
                throw new InvalidOperationException("Order must be Processing to complete.");

            Status = OrderStatus.Completed;
            Touch();
        }

        private void Touch()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        private void AddEvent(OrderEvent domainEvent)
        {
            _events.Add(domainEvent);
        }
    }
}
