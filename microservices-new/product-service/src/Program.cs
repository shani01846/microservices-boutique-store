using System.Text;
using CorrelationId;
using CorrelationId.Abstractions;
using CorrelationId.DependencyInjection;
using CorrelationId.Providers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProductService.Data;
using ProductService.Services;
using Serilog;
using StackExchange.Redis;

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

builder.Services.AddSingleton<ProductRepository>();

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379"));

builder.Services.AddSingleton<ProductCacheService>();

builder.Services.AddHttpClient("inventory", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Inventory"] ?? "http://inventory-service:8080"));

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
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "images"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "images")),
    RequestPath = "/images"
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "product-service" })).AllowAnonymous();

app.Run();
