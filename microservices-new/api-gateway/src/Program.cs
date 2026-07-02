using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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

app.MapReverseProxy();

app.Run();
