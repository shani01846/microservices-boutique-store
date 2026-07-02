using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<NotificationWorker>();
var host = builder.Build();
host.Run();

public class NotificationWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public NotificationWorker(IConfiguration config) => _config = config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);

        _channel!.ExchangeDeclare("order.placed",       ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("inventory.reserved",  ExchangeType.Fanout, durable: true);
        _channel.ExchangeDeclare("inventory.rejected",  ExchangeType.Fanout, durable: true);

        var placedQ   = _channel.QueueDeclare("notification.order.placed",       durable: true, exclusive: false, autoDelete: false);
        var reservedQ = _channel.QueueDeclare("notification.inventory.reserved", durable: true, exclusive: false, autoDelete: false);
        var rejectedQ = _channel.QueueDeclare("notification.inventory.rejected", durable: true, exclusive: false, autoDelete: false);

        _channel.QueueBind(placedQ.QueueName,   "order.placed",       "");
        _channel.QueueBind(reservedQ.QueueName, "inventory.reserved", "");
        _channel.QueueBind(rejectedQ.QueueName, "inventory.rejected", "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            if (ea.Exchange == "order.placed")
            {
                var msg = JsonSerializer.Deserialize<OrderPlacedEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (msg != null)
                {
                    Console.WriteLine($"[Notification] 📧 Sending order-received email to {msg.CustomerEmail} for OrderId={msg.OrderId}");
                    await SendOrderReceivedEmailAsync(msg);
                }
            }
            else
            {
                var msg = JsonSerializer.Deserialize<InventoryEvent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (msg != null)
                {
                    if (ea.Exchange == "inventory.reserved")
                    {
                        Console.WriteLine($"[Notification] ✅ Order {msg.OrderId} CONFIRMED — sending confirmation email to {msg.CustomerEmail}");
                        await SendOrderConfirmedEmailAsync(msg);
                    }
                    else
                    {
                        Console.WriteLine($"[Notification] ❌ Order {msg.OrderId} CANCELLED for user {msg.UserId}. Reason: {msg.Reason}");
                        await SendOrderCancelledEmailAsync(msg);
                    }
                }
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(placedQ.QueueName,   autoAck: false, consumer: consumer);
        _channel.BasicConsume(reservedQ.QueueName, autoAck: false, consumer: consumer);
        _channel.BasicConsume(rejectedQ.QueueName, autoAck: false, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task SendOrderConfirmedEmailAsync(InventoryEvent order)
    {
        var smtpKey = _config["Brevo:SmtpKey"];
        var smtpLogin = _config["Brevo:SmtpLogin"];
        if (string.IsNullOrEmpty(smtpKey) || string.IsNullOrEmpty(smtpLogin))
        {
            Console.WriteLine("[Notification] ⚠️  Brevo SMTP key not configured, skipping email.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("ECommerce Store", "shani01846@gmail.com"));
            message.To.Add(new MailboxAddress(order.CustomerName, order.CustomerEmail));
            message.Subject = $"Order #{order.OrderId} Confirmed!";

            message.Body = new BodyBuilder { HtmlBody = BuildConfirmedHtml(order) }.ToMessageBody();
            message.Headers.Add("X-Mailin-custom",
                $"orderId:{order.OrderId}|customerName:{order.CustomerName}|orderDate:{order.OrderDate:yyyy-MM-dd HH:mm}|totalAmount:{order.TotalAmount:F2}");

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            await client.ConnectAsync("smtp-relay.brevo.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpLogin, smtpKey);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine($"[Notification] ✅ Confirmation email sent to {order.CustomerEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Notification] ❌ Failed to send confirmation email: {ex.Message}");
        }
    }

    private async Task SendOrderCancelledEmailAsync(InventoryEvent order)
    {
        var smtpKey = _config["Brevo:SmtpKey"];
        var smtpLogin = _config["Brevo:SmtpLogin"];
        if (string.IsNullOrEmpty(smtpKey) || string.IsNullOrEmpty(smtpLogin)) return;

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("ECommerce Store", "shani01846@gmail.com"));
            message.To.Add(new MailboxAddress(order.CustomerName, order.CustomerEmail));
            message.Subject = $"Order #{order.OrderId} Cancelled";
            message.Body = new BodyBuilder { HtmlBody = BuildCancelledHtml(order) }.ToMessageBody();

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            await client.ConnectAsync("smtp-relay.brevo.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpLogin, smtpKey);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine($"[Notification] ✅ Cancellation email sent to {order.CustomerEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Notification] ❌ Failed to send cancellation email: {ex.Message}");
        }
    }

    private static string BuildCancelledHtml(InventoryEvent order) => $"""
        <h2>Your order has been cancelled, {order.CustomerName}.</h2>
        <p>Unfortunately, Order <strong>#{order.OrderId}</strong> could not be completed due to a problem with your order.</p>
        <table>
          <tr><td><strong>Order ID:</strong></td><td>{order.OrderId}</td></tr>
          <tr><td><strong>Date:</strong></td><td>{order.OrderDate:yyyy-MM-dd HH:mm} UTC</td></tr>
          <tr><td><strong>Total:</strong></td><td>${order.TotalAmount:F2}</td></tr>
          <tr><td><strong>Status:</strong></td><td>Cancelled ❌</td></tr>
          <tr><td><strong>Reason:</strong></td><td>{order.Reason}</td></tr>
        </table>
        <p>Please contact support if you have any questions.</p>
        """;

    private static string BuildConfirmedHtml(InventoryEvent order) => $"""
        <h2>Your order is confirmed, {order.CustomerName}!</h2>
        <p>Great news! Order <strong>#{order.OrderId}</strong> has been confirmed and is on its way.</p>
        <table>
          <tr><td><strong>Order ID:</strong></td><td>{order.OrderId}</td></tr>
          <tr><td><strong>Date:</strong></td><td>{order.OrderDate:yyyy-MM-dd HH:mm} UTC</td></tr>
          <tr><td><strong>Total:</strong></td><td>${order.TotalAmount:F2}</td></tr>
          <tr><td><strong>Status:</strong></td><td>Confirmed ✅</td></tr>
        </table>
        <p>Thank you for shopping with us!</p>
        """;

    private async Task SendOrderReceivedEmailAsync(OrderPlacedEvent order)
    {
        var smtpKey = _config["Brevo:SmtpKey"];
        var smtpLogin = _config["Brevo:SmtpLogin"];
        if (string.IsNullOrEmpty(smtpKey) || string.IsNullOrEmpty(smtpLogin))
        {
            Console.WriteLine("[Notification] ⚠️  Brevo SMTP key not configured, skipping email.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("ECommerce Store", "shani01846@gmail.com"));
            message.To.Add(new MailboxAddress(order.CustomerName, order.CustomerEmail));
            message.Subject = $"Order #{order.OrderId} Received";

            // Brevo template #1 parameters via X-Mailin headers
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = BuildEmailHtml(order)
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Brevo template substitution headers
            message.Headers.Add("X-Mailin-custom",
                $"orderId:{order.OrderId}|customerName:{order.CustomerName}|orderDate:{order.OrderDate:yyyy-MM-dd HH:mm}|totalAmount:{order.TotalAmount:F2}");

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            await client.ConnectAsync("smtp-relay.brevo.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpLogin, smtpKey);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine($"[Notification] ✅ Email sent to {order.CustomerEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Notification] ❌ Failed to send email: {ex.Message}");
        }
    }

    private static string BuildEmailHtml(OrderPlacedEvent order) => $"""
        <h2>Thank you for your order, {order.CustomerName}!</h2>
        <p>Your order <strong>#{order.OrderId}</strong> has been received and is being processed.</p>
        <table>
          <tr><td><strong>Order ID:</strong></td><td>{order.OrderId}</td></tr>
          <tr><td><strong>Date:</strong></td><td>{order.OrderDate:yyyy-MM-dd HH:mm} UTC</td></tr>
          <tr><td><strong>Total:</strong></td><td>${order.TotalAmount:F2}</td></tr>
          <tr><td><strong>Status:</strong></td><td>Pending confirmation</td></tr>
        </table>
        <p>We will notify you once your order is confirmed.</p>
        """;

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

record OrderPlacedEvent(int OrderId, string UserId, string CustomerEmail, string CustomerName, DateTime OrderDate, decimal TotalAmount);
record InventoryEvent(int OrderId, string UserId, string CustomerEmail, string CustomerName, DateTime OrderDate, decimal TotalAmount, string? Reason);
