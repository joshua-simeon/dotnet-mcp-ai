using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.EntityFrameworkCore;
using McpServer.Data;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    var hostingPort = builder.Configuration["CONTAINER_APP_PORT"]
        ?? builder.Configuration["PORT"];

    if (hostingPort is not null)
    {
        if (!int.TryParse(hostingPort, out var port) || port is < 1 or > 65535)
        {
            throw new InvalidOperationException("The hosting port must be between 1 and 65535.");
        }

        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }
    else if (string.IsNullOrWhiteSpace(builder.Configuration["http_ports"])
        && string.IsNullOrWhiteSpace(builder.Configuration["https_ports"]))
    {
        builder.WebHost.UseUrls("http://0.0.0.0:5000");
    }
}

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithToolsFromAssembly();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<OrderService>();
var connectionString =
    builder.Configuration.GetConnectionString("AzureSql")
    ?? throw new InvalidOperationException(
        "AzureSql connection string is not configured.");

builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseSqlServer(connectionString));
    
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapMcp("/mcp");
await app.RunAsync();
