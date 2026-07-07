using System.Text;
using System.Text.Json;
using CartService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace CartService.Consumers;

public class OrderPlacedConsumer : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderPlacedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

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
        var queue = _channel.QueueDeclare("cart.order.placed", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue.QueueName, "order.placed", "");
        _logger.LogInformation("RabbitMQ queue bound for cart consumer: Queue={Queue}, Exchange={Exchange}", queue.QueueName, "order.placed");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
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

            _logger.LogInformation("RabbitMQ consumed in cart: Exchange={Exchange}, DeliveryTag={DeliveryTag}", ea.Exchange, ea.DeliveryTag);
            var body = JsonSerializer.Deserialize<OrderPlacedEvent>(Encoding.UTF8.GetString(ea.Body.ToArray()));
            if (body != null)
            {
                using var scope = _services.CreateScope();
                var cart = scope.ServiceProvider.GetRequiredService<CartRedisService>();
                await cart.ClearAsync(body.UserId);
                _logger.LogInformation("Cart cleared after order placement for UserId={UserId}", body.UserId);
            }
            else
            {
                _logger.LogWarning("Cart consumer deserialization failed; acking message DeliveryTag={DeliveryTag}", ea.DeliveryTag);
            }
            _channel.BasicAck(ea.DeliveryTag, false);
            _logger.LogInformation("RabbitMQ ack sent in cart consumer: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
        };

        _channel.BasicConsume(queue.QueueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("RabbitMQ consumer started for cart queue: Queue={Queue}", queue.QueueName);
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
                _logger.LogInformation("RabbitMQ connection established for cart consumer");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ connection attempt {Attempt}/10 failed for cart consumer", i + 1);
                await Task.Delay(3000, ct);
            }
        }
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _logger.LogInformation("RabbitMQ connection established for cart consumer after retries");
    }

    public override void Dispose() { _channel?.Dispose(); _connection?.Dispose(); base.Dispose(); }
}

record OrderPlacedEvent(string UserId);
