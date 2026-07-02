using System.Text;
using System.Text.Json;
using CartService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CartService.Consumers;

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
        var queue = _channel.QueueDeclare("cart.order.placed", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue.QueueName, "order.placed", "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var body = JsonSerializer.Deserialize<OrderPlacedEvent>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            if (body != null)
            {
                using var scope = _services.CreateScope();
                var cart = scope.ServiceProvider.GetRequiredService<CartRedisService>();
                await cart.ClearAsync(body.UserId);
            }
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(queue.QueueName, autoAck: false, consumer: consumer);
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

record OrderPlacedEvent(string UserId);
