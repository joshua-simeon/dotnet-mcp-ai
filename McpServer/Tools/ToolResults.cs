using System.Text.Json.Serialization;

namespace McpServer.Tools;

public sealed record CustomerResult(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email);

public sealed record OrderResult(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("customerId")] int CustomerId,
    [property: JsonPropertyName("product")] string Product,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("status")] string Status);
