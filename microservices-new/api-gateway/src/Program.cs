using System.Text;
using CorrelationId;
using CorrelationId.Abstractions;
using CorrelationId.DependencyInjection;
using CorrelationId.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHttpClient("order", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Order"] ?? "http://order-service:8080"));

builder.Services.AddHttpClient("product", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Product"] ?? "http://product-service:8080"));

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ECommerce API Gateway", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

builder.Services.AddHttpClient();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Swagger UI at /swagger
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger-docs/user-service",     "User Service");
    c.SwaggerEndpoint("/swagger-docs/product-service",  "Product Service");
    c.SwaggerEndpoint("/swagger-docs/inventory-service","Inventory Service");
    c.SwaggerEndpoint("/swagger-docs/cart-service",     "Cart Service");
    c.SwaggerEndpoint("/swagger-docs/order-service",    "Order Service");
    c.RoutePrefix = "swagger";
});

// Proxy each service's swagger.json
var services = new Dictionary<string, string>
{
    ["user-service"]      = "http://user-service:8080",
    ["product-service"]   = "http://product-service:8080",
    ["inventory-service"] = "http://inventory-service:8080",
    ["cart-service"]      = "http://cart-service:8080",
    ["order-service"]     = "http://order-service:8080",
};

app.MapGet("/swagger-docs/{serviceName}", async (string serviceName, IHttpClientFactory factory) =>
{
    if (!services.TryGetValue(serviceName, out var baseUrl))
        return Results.NotFound();

    var client = factory.CreateClient();
    var response = await client.GetAsync($"{baseUrl}/swagger/v1/swagger.json");
    if (!response.IsSuccessStatusCode) return Results.StatusCode((int)response.StatusCode);

    var json = await response.Content.ReadAsStringAsync();
    return Results.Content(json, "application/json");
}).AllowAnonymous();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "api-gateway" })).AllowAnonymous();

app.MapGet("/api/bff/order-details/{orderId}", async (string orderId, IHttpClientFactory httpFactory, HttpRequest request) =>
{
    var token = request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

    var orderClient = httpFactory.CreateClient("order");
    orderClient.DefaultRequestHeaders.Add("Authorization", token);

    var orderResponse = await orderClient.GetAsync($"/api/orders/{orderId}");
    if (!orderResponse.IsSuccessStatusCode)
        return Results.StatusCode((int)orderResponse.StatusCode);

    var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailsResponse>();
    if (order == null) return Results.NotFound();

    var productClient = httpFactory.CreateClient("product");
    var items = new List<OrderItemWithProductInfo>();

    foreach (var item in order.Items)
    {
        ProductDetailsResponse? product = null;
        try
        {
            var productResponse = await productClient.GetAsync($"/api/products/{item.ProductId}");
            if (productResponse.IsSuccessStatusCode)
                product = await productResponse.Content.ReadFromJsonAsync<ProductDetailsResponse>();
        }
        catch { }

        items.Add(new OrderItemWithProductInfo(
            item.ProductId,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            product?.Category,
            product?.Size,
            product?.ImageUrl,
            product?.StockQuantity ?? 0));
    }

    return Results.Ok(new BffOrderDetailsResponse(order.Id, order.OrderDate, order.TotalAmount, order.Status, items));
}).RequireAuthorization();

app.MapReverseProxy();

app.Run();

internal record OrderDetailsResponse(int Id, DateTime OrderDate, decimal TotalAmount, string Status, List<OrderItemResponse> Items);
internal record OrderItemResponse(string ProductId, string ProductName, int Quantity, decimal UnitPrice);
internal record ProductDetailsResponse(string Id, string Name, string Description, decimal Price, string Category, string Size, string? ImageUrl, int StockQuantity);
internal record OrderItemWithProductInfo(string ProductId, string ProductName, int Quantity, decimal UnitPrice, string? Category, string? Size, string? ImageUrl, int StockQuantity);
internal record BffOrderDetailsResponse(int Id, DateTime OrderDate, decimal TotalAmount, string Status, List<OrderItemWithProductInfo> Items);
