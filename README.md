# dotnet-mcp-ai
An ASP.NET Core MCP server with customer and order tools, and a Gemini-powered ASP.NET Core web client. Requires .NET 10.
The server runs independently using Streamable HTTP at `/mcp`. The web client keeps the existing agent prompt, Gemini model, conversation history, and automatic tool invocation flow.

## Run the server

Configure the existing Azure SQL connection string, then start the server:

```powershell
dotnet user-secrets set "ConnectionStrings:AzureSql" "<connection-string>" --project McpServer
dotnet run --project McpServer
```

The default listener is `http://0.0.0.0:5000`, accepting connections on all network interfaces.
Connect to `http://localhost:5000/mcp` on the same machine or `http://<server-host>:5000/mcp`
from another machine with network access to port 5000.
Override the listener with `ASPNETCORE_URLS` or `--urls`, for example:

```powershell
dotnet run --project McpServer -- --urls http://127.0.0.1:5000
```

For deployment, set `ConnectionStrings__AzureSql` in the server environment instead of
using development user secrets. Publish with `dotnet publish McpServer -c Release`.
The transport is stateless; conversation history stays in the web client application and
the server does not require session affinity.

The application does not configure authentication. For access outside a trusted private
network, place it behind an HTTPS gateway with authentication and restrict direct access
to the listener. Preserve the `/mcp` route and streaming responses through the gateway.

For the server Docker build, environment variables, health checks, and exact Azure
Container Apps deployment commands, see [Azure Container Apps deployment](docs/azure-container-apps.md).

## Run the web client

Start the server first, then run the client in a separate terminal:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "<api-key>" --project McpClient
dotnet run --project McpClient
```

Open `http://localhost:5001` in your browser. The web listener can be overridden with
`ASPNETCORE_URLS` or `--urls`; it uses a separate port from the MCP server.

The client connects to `http://localhost:5000/mcp` by default. To connect to a remote server:

```powershell
dotnet run --project McpClient -- --McpServer:Endpoint https://<server-host>/mcp
```

Alternatively, set `McpServer:Endpoint` in client user secrets or the environment variable
`McpServer__Endpoint`. The Gemini key also supports `Gemini__ApiKey`.
Command-line settings override environment variables, which override user secrets.
The page lists available tools and accepts requests until you enter `exit`. Select
**New conversation** to reset the history and continue. The Gemini API key and MCP
connection stay on the application server; the browser only calls the web client's endpoints.

The conversion uses minimal HTTP endpoints and a static HTML page, with the agent
setup still in `McpClient/Program.cs`. Each browser session has separate in-memory
conversation history, including tool calls/results. Requests within a session are
serialized. History survives page refreshes, expires after 30 minutes of inactivity,
and is lost on application restart. Multiple tabs in the same browser share the session.
This in-memory client runs as a single application instance.

The web client connects to MCP and discovers tools at startup, so start the MCP server
first. No database or MCP server logic has moved into the web client.
Like the MCP server, the web client has no application authentication; use an
authenticated HTTPS gateway if you expose it outside a trusted private network.
