using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using McpServer.Repositories;
using McpServer.Services;
using Microsoft.EntityFrameworkCore;
using McpServer.Data;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
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
    
await builder.Build().RunAsync();

[McpServerToolType]
public static class GreetingTools
{
    [McpServerTool]
    [Description("Returns a greeting for the specified person. descreption")]
    public static string GetGreeting(
        [Description("The name of the person to greet.")] string name)
    {
        return $"Hello, {name}! Welcome to your first .NET MCP server.";
    }
}

