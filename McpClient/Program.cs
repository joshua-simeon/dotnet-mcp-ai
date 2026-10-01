using ModelContextProtocol.Client;
using Google.GenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Debug);
});

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var apiKey = configuration["Gemini:ApiKey"]
    ?? throw new InvalidOperationException(
        "Gemini API key is not configured.");

var geminiClient = new Client(apiKey: apiKey);
// var response = await geminiClient.Models.GenerateContentAsync(
//     model: "gemini-flash-latest",
//     contents: "Say hello in one sentence.");

// var response = await geminiClient.Models.GenerateContentAsync(
//     model: "gemini-3.6-flash",
//     contents: "Say hello in one sentence.");

// Console.WriteLine(
//     response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
//     ?? string.Empty);

var serverUrl = configuration["McpServer:Endpoint"] ?? "http://localhost:5000/mcp";
if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var serverEndpoint)
    || (serverEndpoint.Scheme != Uri.UriSchemeHttp && serverEndpoint.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("McpServer:Endpoint must be an absolute HTTP or HTTPS URL.");
}

var clientTransport = new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Name = "My MCP Server",
        Endpoint = serverEndpoint,
        TransportMode = HttpTransportMode.StreamableHttp
    });

await using var client = await McpClient.CreateAsync(clientTransport);

Console.WriteLine($"Connected to MCP server at {serverEndpoint}.");
Console.WriteLine();

Console.WriteLine("Available tools:");

var tools = await client.ListToolsAsync();

foreach (var tool in tools)
{
    Console.WriteLine($"- {tool.Name}: {tool.Description}");
}

IChatClient chatClient =
    new Client(apiKey: apiKey)
        .AsIChatClient("gemini-3.8-flash")
        .AsBuilder()
        .UseFunctionInvocation(loggerFactory,
            configure: client =>
            {
                client.MaximumIterationsPerRequest = 10;
            })
        .Build();

var chatOptions = new ChatOptions
{
    Tools = [.. tools]
};

// var responseGoog = await chatClient.GetResponseAsync(
//     "Find the customer named Bob.",
//     chatOptions);

var conversationHistory = new List<ChatMessage>
{
    new(ChatRole.System,
        """
        You are a customer-support agent with access to customer and order data through MCP tools.
        Use MCP tools whenever backend customer or order data must be retrieved, and never invent customer or order data.
        Reuse information already available in the conversation history instead of making redundant tool calls.
        Search by customer name when the customer ID is unknown. Do not search again when the customer ID is already known.
        If multiple customers match and the intended customer cannot be determined, ask the user for clarification.
        Give clear, concise responses based on tool results.
        """)
};

while (true)
{
    Console.Write("Enter your request: ");
    var userPrompt = Console.ReadLine();

    if (userPrompt is null || userPrompt.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    conversationHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

    var responseGoog = await chatClient.GetResponseAsync(conversationHistory, chatOptions);
    conversationHistory.AddMessages(responseGoog);

    Console.WriteLine();
    Console.WriteLine("Gemini response:");
    Console.WriteLine(responseGoog.Text);
    Console.WriteLine();
}

// Console.WriteLine("Non-Gemini calling");
// Console.WriteLine("Calling GetGreeting...");

// var greetingTool = tools.First(t => t.Name.ToLower() == "get_greeting");

// var result = await greetingTool.CallAsync(
//     new Dictionary<string, object?>
//     {
//         ["name"] = "Simeon"
//     });

// Console.WriteLine();
// Console.WriteLine("Result:");

// foreach (var content in result.Content)
// {
//     Console.WriteLine(content);
// }

// Console.WriteLine();
// Console.WriteLine("Calling GetCustomer...");

// var customerResult = await client.CallToolAsync(
//     "get_customer",
//     new Dictionary<string, object?>
//     {
//         ["customerId"] = 1
//     });

// Console.WriteLine();
// Console.WriteLine("Customer result:");

// foreach (var content in customerResult.Content)
// {
//     Console.WriteLine(content);
// }

// Console.WriteLine();
// Console.WriteLine("Calling Search Customer...");

// var customerSearchResult = await client.CallToolAsync(
//     "search_customer_by_name",
//     new Dictionary<string, object?>
//     {
//         ["name"] = "smith"
//     });

// Console.WriteLine();
// Console.WriteLine("Customer result by name:");

// foreach (var content in customerSearchResult.Content)
// {
//     Console.WriteLine(content);
// }
