using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

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
