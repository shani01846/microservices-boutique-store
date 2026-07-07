using CorrelationId;
using CorrelationId.Abstractions;
using CorrelationId.DependencyInjection;
using CorrelationId.Providers;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UserService.Data;
using UserService.Services;

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

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<JwtService>();

var app = builder.Build();

app.UseCorrelationId();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "user-service" })).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
