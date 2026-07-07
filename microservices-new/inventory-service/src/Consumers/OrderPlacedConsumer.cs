using System.Text;
using System.Text.Json;
using Serilog.Context;
using InventoryService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace InventoryService.Consumers;

public class OrderPlacedConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderPlacedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private string? _currentCorrelationId;

    public OrderPlacedConsumer(IServiceProvider services, IConfiguration config, ILogger<OrderPlacedConsumer> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);

        _channel!.ExchangeDeclare("order.placed", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("inventory.reserved", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("inventory.rejected", ExchangeType.Fanout, durable: true);
        _logger.LogInformation("RabbitMQ exchanges declared for inventory consumer");

        var queue = _channel.QueueDeclare("inventory.order.placed", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue.QueueName, "order.placed", "");
        _logger.LogInformation("RabbitMQ queue bound: Queue={Queue}, Exchange={Exchange}", queue.QueueName, "order.placed");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var body = JsonSerializer.Deserialize<OrderPlacedEvent>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            // Extract correlation id from message headers and set in logs
            if (ea.BasicProperties?.Headers != null && ea.BasicProperties.Headers.TryGetValue("X-Correlation-ID", out var corrObj))
            {
                try
                {
                    var corr = Encoding.UTF8.GetString((byte[])corrObj);
                    _currentCorrelationId = corr;
                }
                catch { }
            }
            using var correlationScope = LogContext.PushProperty("CorrelationId", _currentCorrelationId ?? string.Empty);

            _logger.LogInformation("RabbitMQ consumed: Exchange={Exchange}, RoutingKey={RoutingKey}, DeliveryTag={DeliveryTag}", ea.Exchange, ea.RoutingKey, ea.DeliveryTag);

            if (body == null)
            {
                _logger.LogWarning("RabbitMQ message deserialization failed, acking message. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                _channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            _logger.LogInformation("Inventory processing started for OrderId={OrderId}, Items={ItemCount}", body.OrderId, body.Items.Count);

            using var scope = _services.CreateScope();
            var inventory = scope.ServiceProvider.GetRequiredService<InventoryRedisService>();
            var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

            // Idempotency: skip if already processed
            var idempotencyKey = $"saga:inventory:{body.OrderId}";
            if (await redis.KeyExistsAsync(idempotencyKey))
            {
                _logger.LogInformation("Inventory idempotency hit for OrderId={OrderId}; skipping duplicate", body.OrderId);
                _channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            bool allReserved = true;
            var reserved = new List<OrderPlacedItem>();

            foreach (var item in body.Items)
            {
                var success = await inventory.ReserveAsync(item.ProductId, item.Quantity);
                if (success) { reserved.Add(item); }
                else { allReserved = false; break; }
            }

            if (!allReserved)
            {
                foreach (var item in reserved)
                    await inventory.ReleaseAsync(item.ProductId, item.Quantity);

                _logger.LogWarning("Inventory rejected OrderId={OrderId} due to insufficient stock", body.OrderId);
                Publish("inventory.rejected", new { body.OrderId, body.UserId, body.CustomerEmail, body.CustomerName, body.OrderDate, body.TotalAmount, Reason = "Insufficient stock" });
            }
            else
            {
                _logger.LogInformation("Inventory reserved stock for OrderId={OrderId}", body.OrderId);
                Publish("inventory.reserved", new
                {
                    body.OrderId, body.UserId,
                    body.CustomerEmail, body.CustomerName,
                    body.OrderDate, body.TotalAmount
                });
            }

            await redis.StringSetAsync(idempotencyKey, "1", TimeSpan.FromDays(1));
            _channel.BasicAck(ea.DeliveryTag, false);
            _logger.LogInformation("RabbitMQ ack sent for OrderId={OrderId}, DeliveryTag={DeliveryTag}", body.OrderId, ea.DeliveryTag);
        };

        _channel.BasicConsume(queue.QueueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("RabbitMQ consumer started: Queue={Queue}", queue.QueueName);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void Publish(string exchange, object payload)
    {
        using var ch = _connection!.CreateModel();
        var props = ch.CreateBasicProperties();
        props.Persistent = true;
        // propagate correlation id from consumed message to outgoing saga events
        try
        {
            var corr = _currentCorrelationId;
            if (!string.IsNullOrEmpty(corr))
            {
                props.Headers = props.Headers ?? new Dictionary<string, object>();
                props.Headers["X-Correlation-ID"] = Encoding.UTF8.GetBytes(corr);
            }
        }
        catch { }

        ch.BasicPublish(exchange, "", basicProperties: props, body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        _logger.LogInformation("RabbitMQ published: Exchange={Exchange}, PayloadType={PayloadType}", exchange, payload.GetType().Name);
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
                _logger.LogInformation("RabbitMQ connection established for inventory consumer");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection attempt {Attempt}/10 failed for inventory consumer", i + 1);
                await Task.Delay(3000, ct);
            }
        }
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _logger.LogInformation("RabbitMQ connection established for inventory consumer after retries");
    }

    public override void Dispose() { _channel?.Dispose(); _connection?.Dispose(); base.Dispose(); }
}

record OrderPlacedEvent(int OrderId, string UserId, string CustomerEmail, string CustomerName, DateTime OrderDate, decimal TotalAmount, List<OrderPlacedItem> Items);
record OrderPlacedItem(string ProductId, int Quantity);
