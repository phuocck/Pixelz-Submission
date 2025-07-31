# OrderService

A robust .NET backend service for order management in e-commerce environments, built with Clean Architecture principles and designed for internal enterprise use.

## Introduction

OrderService is a critical component of our internal e-commerce ecosystem, responsible for processing customer orders, managing checkout flows, and orchestrating downstream business processes. The service handles order lifecycle management from creation through fulfillment, including payment processing, email notifications, invoice generation, and production system integration.

### Project Goals

- **Reliability**: Ensure consistent order processing with proper error handling and recovery mechanisms
- **Observability**: Provide comprehensive tracing and monitoring for debugging and performance analysis
- **Scalability**: Support high-volume order processing with efficient resource utilization
- **Maintainability**: Follow Clean Architecture principles for easy testing, modification, and extension
The project currently implements a **modular monolith** architecture following Clean Architecture principles. All components (order processing, payment, email, production integration...) are co-located in a single service for simplicity and faster iteration in early stages.

This monolithic structure allows us to:
- Iterate rapidly without infrastructure complexity
- Share code and types across modules
- Simplify local development and testing

Future versions of the system will gradually evolve into an **event-driven architecture**, as detailed in the [Future Enhancements](#future-enhancements) section, to support higher scalability, resilience, and independent deployments.

### Target Audience

This service is designed for internal backend developers working on our e-commerce platform. It serves as a foundational component that other services can integrate with for order-related operations.

## Architecture

### Clean Architecture Implementation

The project follows Clean Architecture principles with clear separation of concerns across four main layers:

```
src/OrderService/
├── OrderService.Api/           # Web layer - Controllers, Middleware, Filters
├── OrderService.Application/   # Use cases, DTOs, Commands, Queries
├── OrderService.Domain/        # Core entities, interfaces, business rules
├── OrderService.Infrastructure/# External services, implementations
└── OrderService.Persistence/   # Data access layer (Repository pattern)
└── OrderService.Application.Test/   # Unit test for Application Layer
```

#### Layer Responsibilities

- **API Layer**: HTTP endpoints, request/response handling, authentication (mocked), rate limiting (mocked)
- **Application Layer**: Business logic orchestration, command/query handling, validation
- **Domain Layer**: Core business entities, interfaces, domain services
- **Infrastructure Layer**: External service integrations, email, payment, production systems
- **Persistence Layer**: Data access abstractions and implementations

### Key Design Decisions

- **Repository Pattern**: Uses in-memory data storage with `OrderRepository` for rapid development
- **Command/Query Separation**: Clear separation between write operations (Commands) and read operations (Queries)
- **Exception Filter**: Centralized error handling with consistent HTTP responses
- **Rate Limiting**: API protection for checkout endpoints to prevent abuse (TODO: implement)
- **OpenTelemetry Integration**: Distributed tracing for observability

## Features

### Current Functionality

#### Order Management
- **Search Orders**: Find orders by name with pagination and filtering
- **Order Details**: Retrieve complete order information by ID
- **Order Status Tracking**: Monitor order lifecycle states

#### Checkout Process
- **Payment Processing**: Secure payment gateway integration (mocked)
- **Email Notifications**: Automatic confirmation emails to customers
- **Invoice Generation**: Create invoices in the invoice system
- **Production Integration**: Push orders to internal production systems

#### Error Handling
- **Idempotent Operations**: Prevent duplicate processing
- **Retry Logic**: Automatic retry for transient failures
- **Graceful Degradation**: Continue processing when non-critical services fail

### API Endpoints

| Endpoint | Method | Description | Rate Limited |
|----------|--------|-------------|--------------|
| `/api/v1/orders?query={name}&page={page}&pageSize={pageSize}` | GET | Search orders by name with pagination | No |
| `/api/v1/orders/{id}` | GET | Get order details by ID | No |
| `/api/v1/orders/checkout` | POST | Process order checkout | Yes (TODO) |
| `/webhook/payment` | POST | Payment status updates (not implemented) | No |
| `/webhook/production` | POST | Production system updates (not implemented) | No |

### Cronjob
  ```CSharp
  RelayOrderStuck.cs //Mocking Cronjob to scan stuck order after service crash to ensure order can be processed
  ```
## Getting Started

### Prerequisites

- **.NET 6 SDK** or higher
- **Visual Studio 2022** or **VS Code**
- **Git** for version control

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/phuocck/Pixelz-Submission.git
   cd Pixelz-Submission
   ```

2. **Build the project**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/OrderService/OrderService.Api
   ```

The API will be available at `https://localhost:5212` (or the configured port).

### Example Usage

#### Search Orders
```bash
curl -X GET "https://localhost:5212/api/v1/orders?query=Summer&page=1&pageSize=10"
```

#### Get Order Details
```bash
curl -X GET "https://localhost:5212/api/v1/orders/{id}"
```

#### Process Checkout
```bash
curl -X POST "https://localhost:5212/api/v1/orders/checkout" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "12345",
  }'
```
Note: Id change when running project, please query first and copy id to request
## Testing & Mock Data

### Repository System

The service uses an `OrderRepository` that simulates database operations with in-memory data storage. This approach enables:

- **Rapid Development**: No database setup required
- **Consistent Testing**: Predictable data for unit and integration tests
- **Easy Demo**: Pre-populated sample data for demonstrations

### Sample Data

The repository is initialized with sample orders on application startup. You can modify the data by editing `OrderRepository.cs`:

```csharp
// Sample orders in OrderRepository
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
```

### Testing Strategy

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Verify component interactions
- **API Tests**: End-to-end testing of HTTP endpoints
- **Mock Services**: External dependencies are mocked for reliable testing

## Development Guidelines

### Code Organization

- **Domain Entities**: Core business objects in `OrderService.Domain/`
- **Use Cases**: Business logic in `OrderService.Application/`
- **DTOs**: Data transfer objects in `OrderService.Application/Dtos/`
- **Repositories**: Data access interfaces in `OrderService.Domain/`
- **Controllers**: HTTP endpoints in `OrderService.Api/Controllers/`

### Error Handling

The service implements a centralized exception filter that:

- **Logs Errors**: Comprehensive error logging with context
- **Returns Consistent Responses**: Standardized error response format
- **Handles Different Exception Types**: Specific handling for business vs. technical errors
- **Provides Traceability**: Correlation IDs for debugging

### Rate Limiting

Checkout endpoints are protected with rate limiting to prevent abuse:

- **Status**: TODO - Implementation pending
- **Planned Limit**: 10 requests per minute per IP
- **Configurable**: Limits can be adjusted in `appsettings.json`
- **Graceful Handling**: Returns 429 Too Many Requests when limit exceeded

## Future Enhancements

### Phase 2: Event-Driven Architecture

Based on the system design, the next phase will implement an event-driven architecture for better scalability and resilience:

#### Event-Driven Design Overview

- **Payment Success Events**: Write `OrderPaid` events to outbox
- **Change Data Capture (CDC)**: Publish events from database changes
- **Independent Handlers**: Separate handlers for Email, Invoice, and Production services
- **Event Persistence**: Durable event storage with outbox pattern
New structure:
src/

  ├── OrderService		    \# Management Order				   
  	├── API/                \# Web layer, controllers  
  	├── Application/        \# Use cases, DTOs, retry logic  
  	├── Domain/             \# Core entities, enums, interfaces  
  ├── PaymentService/        \# Separate payment domain  
  ├── InvoiceService/        \# Separate invoice domain  
  ├── ProductionService/     \# Separate production domain (optional)
#### Benefits of Event-Driven Architecture

- **Data Consistency**: Local transactions ensure no events are lost
- **Scalability**: Each handler can scale independently
- **Resilience**: Retry logic isolated per handler
- **Extensibility**: Easy to add new handlers (SMS, Slack, etc.)

#### Error Handling in Event-Driven Mode

- **Idempotent Processing**: Each handler is idempotent and safely retryable
- **Persistent Events**: Events written to durable storage before processing
- **Recovery Mechanisms**: Background jobs for stuck orders
- **Event Replay**: Failed events can be reprocessed

### Planned Improvements

- **Event-Driven Architecture**: Migrate to event-driven model for better scalability
- **Database Integration**: Replace in-memory repository with Entity Framework Core
- **Message Queues**: Implement async processing with message queues
- **Microservices**: Split into domain-specific services
- **Caching**: Add Redis caching for improved performance

### Scalability Considerations

- **Database Sharding**: Partition orders by creation time
- **Read/Write Separation**: Separate read and write databases
- **Elasticsearch Integration**: Full-text search capabilities
- **Background Jobs**: Async processing for non-critical operations
- **Event Sourcing**: Consider event sourcing for audit trails

## Monitoring & Observability

### OpenTelemetry Integration

The service includes comprehensive tracing and metrics:

- **Distributed Tracing**: Track requests across service boundaries
- **Performance Metrics**: Monitor response times and throughput
- **Error Tracking**: Capture and analyze error patterns
- **Business Metrics**: Order processing statistics

### Logging

- **Structured Logging**: JSON-formatted logs for easy parsing
- **Log Levels**: Appropriate log levels for different environments
- **Correlation IDs**: Track requests across log entries
- **Sensitive Data Protection**: Automatic masking of sensitive information

## Contributing

### Development Workflow

1. **Feature Branches**: Create feature branches from `main`
2. **Code Review**: All changes require pull request review
3. **Testing**: Ensure all tests pass before merging
4. **Documentation**: Update documentation for new features

### Code Standards

- **C# Coding Conventions**: Follow Microsoft C# coding conventions
- **SOLID Principles**: Apply SOLID principles in design
- **Clean Code**: Write readable, maintainable code
- **Documentation**: Include XML documentation for public APIs

## Support

For questions, issues, or contributions:

- **Internal Documentation**: Check the project wiki
- **Team Chat**: Use the dedicated development channel
- **Issue Tracking**: Create issues in the project repository
- **Code Reviews**: Request reviews from team members

---

**Note**: This service is designed for internal use and does not include production-ready integrations. External service connections (email, payment, production systems) are mocked for development and testing purposes. 