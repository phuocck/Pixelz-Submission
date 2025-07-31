using OrderService.Domain;
using OrderService.Api.ExceptionFilter;
using OrderService.Application.Commands;
using OrderService.Application.Dtos.Checkout;
using OrderService.Application.Queries;
using OrderService.Persistence.DbContexts;
using OrderService.Persistence.Repositories;
using OrderService.Infrastructure.Mocks;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add<OrderExceptionFilter>();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//TODO => add ratelimit for checkout
//builder.Services.AddRateLimiter(_ => _
//    .AddPolicy("CheckoutPolicy", context =>
//    {
//        return RateLimitPartition.GetRemoteIpAddressLimiter(
//            context.HttpContext,
//            _ => new FixedWindowRateLimiterOptions
//            {
//                PermitLimit = 5, // cho phép 5 requests
//                Window = TimeSpan.FromMinutes(1), // mỗi phút
//                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
//                QueueLimit = 0 // không queue nếu vượt quá
//            });
//    }));

InjectOrderUsecase(builder);
InjectOrderRepository(builder);
InjectOrderInfrastructure(builder);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

static void InjectOrderUsecase(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<IOrderCommand<CheckoutDtoCommand, CheckoutDtoReponse>, CheckoutCommandHandler>();
    builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();
}

static void InjectOrderRepository(WebApplicationBuilder builder)
{
    //Singleton for mock data
    builder.Services.AddSingleton<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<OrderDbContext>();
}

static void InjectOrderInfrastructure(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<IPaymentGateway, PaymentGateway>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IProductionClient, ProductionClient>();
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();
}