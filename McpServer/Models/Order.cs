namespace McpServer.Models;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string Product { get; set; } = "";

    public decimal Amount { get; set; }

    public string Status { get; set; } = "";
}