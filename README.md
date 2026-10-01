# dotnet-mcp-ai
An ASP.NET Core MCP server with customer and order tools, and a Gemini-powered console client. Requires .NET 10.
The server runs independently using Streamable HTTP at `/mcp`. The console client keeps its interactive chat and tool invocation flow.

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
The transport is stateless; conversation history stays in the console client and
the server does not require session affinity.

The application does not configure authentication. For access outside a trusted private
network, place it behind an HTTPS gateway with authentication and restrict direct access
to the listener. Preserve the `/mcp` route and streaming responses through the gateway.

## Run the console client

Start the server first, then run the client in a separate terminal:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "<api-key>" --project McpClient
dotnet run --project McpClient
```

The client defaults to `http://localhost:5000/mcp`. To connect to a remote server:

```powershell
dotnet run --project McpClient -- --McpServer:Endpoint https://<server-host>/mcp
```

Alternatively, set `McpServer:Endpoint` in client user secrets or the environment variable
`McpServer__Endpoint`. The Gemini key also supports `Gemini__ApiKey`.
Command-line settings override environment variables, which override user secrets.
The client lists available tools and accepts requests until you enter `exit`.
It no longer launches a server process.
