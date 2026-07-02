using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Consumers;

public class InventoryResponseConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public InventoryResponseConsumer(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);

        _channel!.ExchangeDeclare("inventory.reserved", ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("inventory.rejected", ExchangeType.Fanout, durable: true);

        var reservedQ = _channel.QueueDeclare("order.inventory.reserved", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(reservedQ.QueueName, "inventory.reserved", "");

        var rejectedQ = _channel.QueueDeclare("order.inventory.rejected", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(rejectedQ.QueueName, "inventory.rejected", "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var msg = JsonSerializer.Deserialize<InventoryResponseEvent>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            if (msg == null) { _channel.BasicAck(ea.DeliveryTag, false); return; }

            var newStatus = ea.Exchange == "inventory.reserved" ? "Confirmed" : "Cancelled";
            Console.WriteLine($"[Order] OrderId={msg.OrderId} → {newStatus}" +
                (newStatus == "Cancelled" ? $" (Reason: {msg.Reason})" : ""));

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == msg.OrderId);
            if (order != null && order.Status == "Pending")
            {
                order.Status = newStatus;
                await db.SaveChangesAsync();
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(reservedQ.QueueName, autoAck: false, consumer: consumer);
        _channel.BasicConsume(rejectedQ.QueueName, autoAck: false, consumer: consumer);

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
            try { _connection = factory.CreateConnection(); _channel = _connection.CreateModel(); return; }
            catch { await Task.Delay(3000, ct); }
        }
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public override void Dispose() { _channel?.Dispose(); _connection?.Dispose(); base.Dispose(); }
}

record InventoryResponseEvent(int OrderId, string UserId, string? Reason);
