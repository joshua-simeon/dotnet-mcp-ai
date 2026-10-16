using ModelContextProtocol.Client;
using Google.GenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);
builder.Logging.ClearProviders().AddConsole().SetMinimumLevel(LogLevel.Debug);

if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://localhost:5001");
}

builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "McpClient.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var apiKey = builder.Configuration["Gemini:ApiKey"]
    ?? throw new InvalidOperationException("Gemini API key is not configured.");

var serverUrl = builder.Configuration["McpServer:Endpoint"] ?? "http://localhost:5000/mcp";
if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var serverEndpoint)
    || (serverEndpoint.Scheme != Uri.UriSchemeHttp && serverEndpoint.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("McpServer:Endpoint must be an absolute HTTP or HTTPS URL.");
}

var clientTransport = new HttpClientTransport(new HttpClientTransportOptions
{
    Name = "My MCP Server",
    Endpoint = serverEndpoint,
    TransportMode = HttpTransportMode.StreamableHttp
});

await using var client = await McpClient.CreateAsync(clientTransport);
var tools = await client.ListToolsAsync();

await using var app = builder.Build();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
app.Logger.LogInformation("Connected to MCP server at {Endpoint}.", serverEndpoint);

using IChatClient chatClient = new Client(apiKey: apiKey)
    .AsIChatClient("gemini-3.8-flash")
    .AsBuilder()
    .UseFunctionInvocation(loggerFactory, configure: client =>
    {
        client.MaximumIterationsPerRequest = 10;
    })
    .Build();

var chatOptions = new ChatOptions { Tools = [.. tools] };
var conversations = app.Services.GetRequiredService<IMemoryCache>();
var conversationCreationLock = new object();

ChatConversation GetConversation(HttpContext context)
{
    // Establish the session cookie before responding; never share history across browsers.
    context.Session.SetString("Active", "true");
    lock (conversationCreationLock)
    {
        return conversations.GetOrCreate(context.Session.Id, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            return new ChatConversation();
        })!;
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSession();

app.MapGet("/api/chat", async (HttpContext context) =>
{
    var conversation = GetConversation(context);
    await conversation.Gate.WaitAsync(context.RequestAborted);
    try
    {
        return Results.Ok(new
        {
            tools = tools.Select(tool => new { tool.Name, tool.Description }),
            messages = conversation.History
                .Where(message => (message.Role == ChatRole.User || message.Role == ChatRole.Assistant)
                    && !string.IsNullOrEmpty(message.Text))
                .Select(message => new { role = message.Role.Value, text = message.Text }),
            ended = conversation.Ended
        });
    }
    finally
    {
        conversation.Gate.Release();
    }
});

app.MapPost("/api/chat", async (ChatRequest request, HttpContext context) =>
{
    if (string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.BadRequest(new { error = "Enter a request." });
    }

    var conversation = GetConversation(context);
    await conversation.Gate.WaitAsync(context.RequestAborted);
    try
    {
        if (conversation.Ended)
        {
            return Results.BadRequest(new { error = "Start a new conversation to continue." });
        }

        if (request.Prompt.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            conversation.Ended = true;
            return Results.Ok(new { text = "Conversation ended.", ended = true });
        }

        // Commit only successful turns, including tool calls/results needed by subsequent turns.
        var history = new List<ChatMessage>(conversation.History)
        {
            new(ChatRole.User, request.Prompt)
        };
        var response = await chatClient.GetResponseAsync(history, chatOptions,
            context.RequestAborted);
        history.AddMessages(response);
        conversation.History = history;
        return Results.Ok(new { text = response.Text, ended = false });
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        throw;
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Agent request failed.");
        return Results.Problem("The agent request failed. Please try again.", statusCode: 502);
    }
    finally
    {
        conversation.Gate.Release();
    }
});

app.MapPost("/api/chat/reset", async (HttpContext context) =>
{
    var conversation = GetConversation(context);
    await conversation.Gate.WaitAsync(context.RequestAborted);
    try
    {
        conversation.History = ChatConversation.CreateHistory();
        conversation.Ended = false;
        return Results.NoContent();
    }
    finally
    {
        conversation.Gate.Release();
    }
});

await app.RunAsync();

record ChatRequest(string? Prompt);

sealed class ChatConversation
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public List<ChatMessage> History { get; set; } = CreateHistory();
    public bool Ended { get; set; }

    public static List<ChatMessage> CreateHistory() =>
    [
        new(ChatRole.System,
            """
            You are a customer-support agent with access to customer and order data through MCP tools.
            Use MCP tools whenever backend customer or order data must be retrieved, and never invent customer or order data.
            Reuse information already available in the conversation history instead of making redundant tool calls.
            Search by customer name when the customer ID is unknown. Do not search again when the customer ID is already known.
            If multiple customers match and the intended customer cannot be determined, ask the user for clarification.
            Give clear, concise responses based on tool results.
            """)
    ];
}
