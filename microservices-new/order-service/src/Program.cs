using System.Text;
using CorrelationId;
using CorrelationId.Abstractions;
using CorrelationId.DependencyInjection;
using CorrelationId.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Services;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var seqServer = builder.Configuration["Seq:ServerUrl"] ?? "http://seq:80";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .WriteTo.Console()
    .WriteTo.Seq(seqServer)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationId(options =>
{
    options.IncludeInResponse = true;
    options.UpdateTraceIdentifier = true;
});
builder.Services.AddSingleton<ICorrelationIdProvider, GuidCorrelationIdProvider>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq",
        UserName = builder.Configuration["RabbitMQ:User"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest"
    };
    // Retry until RabbitMQ is ready
    for (int i = 0; i < 10; i++)
    {
        try { return factory.CreateConnection(); }
        catch { Thread.Sleep(3000); }
    }
    return factory.CreateConnection();
});

builder.Services.AddScoped<OrderBusinessService>();
builder.Services.AddHostedService<InventoryResponseConsumer>();

builder.Services.AddHttpClient("cart", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Cart"] ?? "http://cart-service:8080"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                builder.Configuration["Jwt:SecretKey"] ?? "MyVeryLongSecretKeyForJWTTokenGeneration123456789!")),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "order-service" })).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
