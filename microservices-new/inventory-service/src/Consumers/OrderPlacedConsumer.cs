using System.Text;
using System.Text.Json;
using InventoryService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace InventoryService.Consumers;

public class OrderPlacedConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public OrderPlacedConsumer(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);

        _channel!.ExchangeDeclare("order.placed", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("inventory.reserved", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("inventory.rejected", ExchangeType.Fanout, durable: true);

        var queue = _channel.QueueDeclare("inventory.order.placed", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue.QueueName, "order.placed", "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var body = JsonSerializer.Deserialize<OrderPlacedEvent>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            if (body == null) { _channel.BasicAck(ea.DeliveryTag, false); return; }

            using var scope = _services.CreateScope();
            var inventory = scope.ServiceProvider.GetRequiredService<InventoryRedisService>();
            var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>().GetDatabase();

            // Idempotency: skip if already processed
            var idempotencyKey = $"saga:inventory:{body.OrderId}";
            if (await redis.KeyExistsAsync(idempotencyKey))
            {
                Console.WriteLine($"[Inventory] Duplicate message for OrderId={body.OrderId}, skipping.");
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

                Console.WriteLine($"[Inventory] REJECTED OrderId={body.OrderId} — insufficient stock.");
                Publish("inventory.rejected", new { body.OrderId, body.UserId, body.CustomerEmail, body.CustomerName, body.OrderDate, body.TotalAmount, Reason = "Insufficient stock" });
            }
            else
            {
                Console.WriteLine($"[Inventory] RESERVED stock for OrderId={body.OrderId}.");
                Publish("inventory.reserved", new
                {
                    body.OrderId, body.UserId,
                    body.CustomerEmail, body.CustomerName,
                    body.OrderDate, body.TotalAmount
                });
            }

            await redis.StringSetAsync(idempotencyKey, "1", TimeSpan.FromDays(1));
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(queue.QueueName, autoAck: false, consumer: consumer);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void Publish(string exchange, object payload)
    {
        using var ch = _connection!.CreateModel();
        ch.BasicPublish(exchange, "", body: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
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
            try { _connection = factory.CreateConnection(); _channel = _connection.CreateModel(); return; }
            catch { await Task.Delay(3000, ct); }
        }
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public override void Dispose() { _channel?.Dispose(); _connection?.Dispose(); base.Dispose(); }
}

record OrderPlacedEvent(int OrderId, string UserId, string CustomerEmail, string CustomerName, DateTime OrderDate, decimal TotalAmount, List<OrderPlacedItem> Items);
record OrderPlacedItem(string ProductId, int Quantity);
