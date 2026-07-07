using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace OrderService.Consumers;

public class InventoryResponseConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<InventoryResponseConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public InventoryResponseConsumer(IServiceProvider services, IConfiguration config, ILogger<InventoryResponseConsumer> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);

        _channel!.ExchangeDeclare("inventory.reserved", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("inventory.rejected", ExchangeType.Fanout, durable: true);
        _logger.LogInformation("RabbitMQ exchanges declared for order response consumer");

        var reservedQ = _channel.QueueDeclare("order.inventory.reserved", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(reservedQ.QueueName, "inventory.reserved", "");

        var rejectedQ = _channel.QueueDeclare("order.inventory.rejected", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(rejectedQ.QueueName, "inventory.rejected", "");
        _logger.LogInformation("RabbitMQ queues bound: {ReservedQueue}, {RejectedQueue}", reservedQ.QueueName, rejectedQ.QueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var msg = JsonSerializer.Deserialize<InventoryResponseEvent>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            // Log correlation id if present on message
            if (ea.BasicProperties?.Headers != null && ea.BasicProperties.Headers.TryGetValue("X-Correlation-ID", out var corrObj))
            {
                try
                {
                    var corr = Encoding.UTF8.GetString((byte[])corrObj);
                    using var __ = LogContext.PushProperty("CorrelationId", corr);
                    _logger.LogInformation("RabbitMQ consumed with CorrelationId={CorrelationId}", corr);
                }
                catch { }
            }
            _logger.LogInformation("RabbitMQ consumed: Exchange={Exchange}, RoutingKey={RoutingKey}, DeliveryTag={DeliveryTag}", ea.Exchange, ea.RoutingKey, ea.DeliveryTag);

            if (msg == null)
            {
                _logger.LogWarning("Inventory response deserialization failed, acking message. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                _channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            var newStatus = ea.Exchange == "inventory.reserved" ? "Confirmed" : "Cancelled";
            _logger.LogInformation("Order status transition from inventory event: OrderId={OrderId}, NewStatus={NewStatus}, Reason={Reason}", msg.OrderId, newStatus, msg.Reason);

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == msg.OrderId);
            if (order != null && order.Status == "Pending")
            {
                order.Status = newStatus;
                await db.SaveChangesAsync();
                _logger.LogInformation("Order persisted: OrderId={OrderId}, Status={Status}", msg.OrderId, newStatus);
            }
            else
            {
                _logger.LogInformation("Order status update skipped: OrderId={OrderId}, OrderFound={OrderFound}, CurrentStatus={CurrentStatus}", msg.OrderId, order != null, order?.Status);
            }

            _channel.BasicAck(ea.DeliveryTag, false);
            _logger.LogInformation("RabbitMQ ack sent: DeliveryTag={DeliveryTag}, OrderId={OrderId}", ea.DeliveryTag, msg.OrderId);
        };

        _channel.BasicConsume(reservedQ.QueueName, autoAck: false, consumer: consumer);
        _channel.BasicConsume(rejectedQ.QueueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("RabbitMQ consumer started for order response queues");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConnectWithRetryAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "rabbitmq",
            UserName = _config["RabbitMQ:User"] ?? "guest",
            Password = _config["RabbitMQ:Password"] ?? "guest",
            DispatchConsumersAsync = true
        };
        for (int i = 0; i < 10; i++)
        {
            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _logger.LogInformation("RabbitMQ connection established for order response consumer");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection attempt {Attempt}/10 failed for order response consumer", i + 1);
                await Task.Delay(3000, ct);
            }
        }
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _logger.LogInformation("RabbitMQ connection established for order response consumer after retries");
    }

    public override void Dispose() { _channel?.Dispose(); _connection?.Dispose(); base.Dispose(); }
}

record InventoryResponseEvent(int OrderId, string UserId, string? Reason);
