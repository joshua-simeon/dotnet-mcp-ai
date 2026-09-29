using ModelContextProtocol.Client;

var clientTransport = new StdioClientTransport(
    new StdioClientTransportOptions
    {
        Name = "My MCP Server",
        Command = "dotnet",
        Arguments =
        [
            "run",
            "--project",
            "../McpServer/McpServer.csproj"
        ]
    });

await using var client = await McpClient.CreateAsync(clientTransport);

Console.WriteLine("Connected to MCP server.");
Console.WriteLine();

Console.WriteLine("Available tools:");

var tools = await client.ListToolsAsync();

foreach (var tool in tools)
{
    Console.WriteLine($"- {tool.Name}: {tool.Description}");
}

Console.WriteLine();
Console.WriteLine("Calling GetGreeting...");

var greetingTool = tools.First(t => t.Name.ToLower() == "get_greeting");

var result = await greetingTool.CallAsync(
    new Dictionary<string, object?>
    {
        ["name"] = "Simeon"
    });

Console.WriteLine();
Console.WriteLine("Result:");

foreach (var content in result.Content)
{
    Console.WriteLine(content);
}

Console.WriteLine();
Console.WriteLine("Calling GetCustomer...");

var customerResult = await client.CallToolAsync(
    "get_customer",
    new Dictionary<string, object?>
    {
        ["customerId"] = 1
    });

Console.WriteLine();
Console.WriteLine("Customer result:");

foreach (var content in customerResult.Content)
{
    Console.WriteLine(content);
}