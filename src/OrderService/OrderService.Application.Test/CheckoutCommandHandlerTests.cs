using FluentAssertions;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Dtos.Checkout;
using OrderService.Application.Exceptions;
using OrderService.Application.Test.TestHelpers;
using OrderService.Domain;
using OrderService.Domain.Events;
using OrderSevice.Infrastructure.Mocks;

namespace OrderService.Application.Test
{
    /// <summary>
    /// Mocking for some test, not are all test cover
    /// </summary>
    [TestFixture]
    public class CheckoutCommandHandlerTests
    {
        private Mock<IOrderRepository> _mockOrderRepository;
        private Mock<IEmailService> _mockEmailService;
        private Mock<IProductionClient> _mockProductionClient;
        private Mock<IPaymentGateway> _mockPaymentGateway;
        private Mock<IInvoiceService> _mockInvoiceService;
        private CheckoutCommandHandler _handler;
        private OrderEntity _testOrder;
        private Guid _testOrderId;
        private Guid _testUserId;

        [SetUp]
        public void Setup()
        {
            _mockOrderRepository = new Mock<IOrderRepository>();
            _mockEmailService = new Mock<IEmailService>();
            _mockProductionClient = new Mock<IProductionClient>();
            _mockPaymentGateway = new Mock<IPaymentGateway>();
            _mockInvoiceService = new Mock<IInvoiceService>();

            _handler = new CheckoutCommandHandler(
                _mockOrderRepository.Object,
                _mockEmailService.Object,
                _mockProductionClient.Object,
                _mockPaymentGateway.Object,
                _mockInvoiceService.Object);

            _testOrderId = Guid.NewGuid();
            _testUserId = Guid.Parse("0d567094-fe45-4d89-a338-5ca07e68a340");
            _testOrder = OrderEntityTestHelper.CreateTestOrder("Test Order", 100.00m, _testOrderId, _testUserId);
        }

        [Test]
        public async Task ExecuteAsync_WithValidOrder_ShouldCompleteCheckoutSuccessfully()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            _mockOrderRepository.Verify(x => x.GetByIdAsync(_testOrderId), Times.Once);
            _mockOrderRepository.Verify(x => x.SaveChangesAsync(), Times.Exactly(3)); // Payment pending, paid, and completed
            _mockPaymentGateway.Verify(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount), Times.Once);
            _mockOrderRepository.Verify(x => x.AddOutboxEvent(It.IsAny<IReadOnlyCollection<OrderEvent>>()), Times.Once);
            _mockEmailService.Verify(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()), Times.Once);
            _mockInvoiceService.Verify(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount), Times.Once);
            _mockProductionClient.Verify(x => x.PushAsync(_testOrderId), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WhenOrderNotFound_ShouldThrowNotFoundException()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync((OrderEntity?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<NotFoundException>(
                async () => await _handler.ExecuteAsync(command));
            
            exception.Message.Should().Be("Order not found");
            _mockOrderRepository.Verify(x => x.GetByIdAsync(_testOrderId), Times.Once);
            _mockPaymentGateway.Verify(x => x.ChargeAsync(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_WhenOrderStatusIsNotCreated_ShouldThrowInvalidOrderStateException()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            var orderWithInvalidStatus = OrderEntityTestHelper.CreateTestOrderWithStatus("Test Order", 100.00m, OrderStatus.PaymentPending, _testOrderId, _testUserId);
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(orderWithInvalidStatus);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOrderStateException>(
                async () => await _handler.ExecuteAsync(command));
            
            exception.Message.Should().Be("Order is not in a valid state for checkout");
            _mockPaymentGateway.Verify(x => x.ChargeAsync(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_WhenPaymentFails_ShouldThrowPaymentFailedException()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<PaymentFailedException>(
                async () => await _handler.ExecuteAsync(command));
            
            exception.Message.Should().Be("Payment was declined by provider");
            _mockPaymentGateway.Verify(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount), Times.Once);
            _mockOrderRepository.Verify(x => x.SaveChangesAsync(), Times.Once); // Only payment failed save
        }

        [Test]
        public async Task ExecuteAsync_WhenEmailServiceFails_ShouldMarkOrderAsProcessingFailed()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service unavailable"));

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Main checkout still succeeds
            
            _mockOrderRepository.Verify(x => x.SaveChangesAsync(), Times.Exactly(3)); // Payment pending, paid, processing (processing failed is not saved)
            _mockEmailService.Verify(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()), Times.Exactly(3)); // Retry attempts
        }

        [Test]
        public async Task ExecuteAsync_WhenInvoiceServiceFails_ShouldMarkOrderAsProcessingFailed()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount))
                .ThrowsAsync(new Exception("Invoice service unavailable"));

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Main checkout still succeeds
            
            _mockInvoiceService.Verify(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount), Times.Exactly(3)); // Retry attempts
        }

        [Test]
        public async Task ExecuteAsync_WhenProductionClientFails_ShouldMarkOrderAsProcessingFailed()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .ThrowsAsync(new Exception("Production service unavailable"));

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue(); // Main checkout still succeeds
            
            _mockProductionClient.Verify(x => x.PushAsync(_testOrderId), Times.Exactly(2)); // Retry attempts
        }

        [Test]
        public async Task ExecuteAsync_WhenAllPostPaymentStepsSucceed_ShouldMarkOrderAsCompleted()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            _mockOrderRepository.Verify(x => x.SaveChangesAsync(), Times.Exactly(3)); // Payment pending, paid, completed
        }

        [Test]
        public async Task ExecuteAsync_ShouldAddOutboxEventsAfterPaymentSuccess()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            _mockOrderRepository.Verify(x => x.AddOutboxEvent(It.IsAny<IReadOnlyCollection<OrderEvent>>()), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WhenRepositorySaveChangesFails_ShouldThrowException()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<PaymentFailedException>(
                async () => await _handler.ExecuteAsync(command));
            
            _mockOrderRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WhenEmailServiceSucceedsAfterRetries_ShouldCompleteSuccessfully()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            var callCount = 0;
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(() => 
                {
                    callCount++;
                    if (callCount < 3)
                        throw new Exception("Email service temporarily unavailable");
                    return Task.CompletedTask;
                });
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            callCount.Should().Be(3);
            _mockEmailService.Verify(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()), Times.Exactly(3));
        }

        [Test]
        public async Task ExecuteAsync_WhenInvoiceServiceSucceedsAfterRetries_ShouldCompleteSuccessfully()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            var callCount = 0;
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount))
                .Returns(() => 
                {
                    callCount++;
                    if (callCount < 3)
                        throw new Exception("Invoice service temporarily unavailable");
                    return Task.FromResult(true);
                });
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            callCount.Should().Be(3);
            _mockInvoiceService.Verify(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount), Times.Exactly(3));
        }

        [Test]
        public async Task ExecuteAsync_WhenProductionClientSucceedsAfterRetries_ShouldCompleteSuccessfully()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            var callCount = 0;
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, _testOrder.TotalAmount))
                .ReturnsAsync(true);
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .Returns(() => 
                {
                    callCount++;
                    if (callCount < 2)
                        throw new Exception("Production service temporarily unavailable");
                    return Task.FromResult(true);
                });

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            callCount.Should().Be(2);
            _mockProductionClient.Verify(x => x.PushAsync(_testOrderId), Times.Exactly(2));
        }

        [Test]
        public async Task ExecuteAsync_WithZeroAmountOrder_ShouldProcessSuccessfully()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            var zeroAmountOrder = OrderEntityTestHelper.CreateTestOrder("Free Order", 0.00m, _testOrderId, _testUserId);
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(zeroAmountOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, 0.00m))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, 0.00m))
                .ReturnsAsync(true);
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            _mockPaymentGateway.Verify(x => x.ChargeAsync(_testOrderId, 0.00m), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WithHighAmountOrder_ShouldProcessSuccessfully()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            var highAmountOrder = OrderEntityTestHelper.CreateTestOrder("Expensive Order", 999999.99m, _testOrderId, _testUserId);
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(highAmountOrder);
            _mockOrderRepository.Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, 999999.99m))
                .ReturnsAsync(true);
            _mockEmailService.Setup(x => x.SendEmailAsync(_testOrderId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _mockInvoiceService.Setup(x => x.CreateInvoiceAsync(_testOrderId, 999999.99m))
                .ReturnsAsync(true);
            _mockProductionClient.Setup(x => x.PushAsync(_testOrderId))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.ExecuteAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            _mockPaymentGateway.Verify(x => x.ChargeAsync(_testOrderId, 999999.99m), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_WithDifferentOrderStatuses_ShouldThrowInvalidOrderStateException()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            var invalidStatuses = new[] { OrderStatus.PaymentPending, OrderStatus.PaymentFailed, OrderStatus.Paid, OrderStatus.Processing, OrderStatus.ProcessingFailed, OrderStatus.Completed };

            foreach (var status in invalidStatuses)
            {
                var orderWithInvalidStatus = OrderEntityTestHelper.CreateTestOrderWithStatus("Test Order", 100.00m, status, _testOrderId, _testUserId);
                
                _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                    .ReturnsAsync(orderWithInvalidStatus);

                // Act & Assert
                var exception = Assert.ThrowsAsync<InvalidOrderStateException>(
                    async () => await _handler.ExecuteAsync(command));
                
                exception.Message.Should().Be("Order is not in a valid state for checkout");
                _mockPaymentGateway.Verify(x => x.ChargeAsync(It.IsAny<Guid>(), It.IsAny<decimal>()), Times.Never);
                
                // Reset mocks for next iteration
                _mockOrderRepository.Reset();
                _mockPaymentGateway.Reset();
            }
        }

        [Test]
        public async Task ExecuteAsync_WhenPaymentGatewayThrowsException_ShouldPropagateException()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ReturnsAsync(_testOrder);
            _mockPaymentGateway.Setup(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount))
                .ThrowsAsync(new Exception("Payment gateway connection failed"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(
                async () => await _handler.ExecuteAsync(command));
            
            exception.Message.Should().Be("Payment gateway connection failed");
            _mockPaymentGateway.Verify(x => x.ChargeAsync(_testOrderId, _testOrder.TotalAmount), Times.Exactly(3)); // Retry attempts
        }

        [Test]
        public async Task ExecuteAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var command = new CheckoutDtoCommand { OrderId = _testOrderId };
            
            _mockOrderRepository.Setup(x => x.GetByIdAsync(_testOrderId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(
                async () => await _handler.ExecuteAsync(command));
            
            exception.Message.Should().Be("Database connection failed");
            _mockOrderRepository.Verify(x => x.GetByIdAsync(_testOrderId), Times.Once);
        }
    }
} 