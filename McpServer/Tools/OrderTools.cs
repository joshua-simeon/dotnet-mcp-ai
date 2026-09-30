using System.ComponentModel;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class OrderTools
{
    [McpServerTool]
    [Description("Gets order information by order ID.")]
    public static string GetOrder(
        OrderService orderService,
        [Description("The unique ID of the order.")] int orderId)
    {
        var order = orderService.GetOrder(orderId);

        if (order == null)
        {
            return $"Order ID {orderId} was not found.";
        }

        return $"Order ID: {order.Id}, " +
               $"Customer ID: {order.CustomerId}, " +
               $"Product: {order.Product}, " +
               $"Amount: ${order.Amount:F2}, " +
               $"Status: {order.Status}";
    }
    [McpServerTool]
    [Description("Gets all orders for a specific customer.")]
    public static string GetOrdersByCustomer(
    OrderService orderService,
    [Description("The unique ID of the customer.")] int customerId)
    {
        var orders = orderService.GetOrdersByCustomerId(customerId);

        if (orders.Count == 0)
        {
            return $"No orders were found for customer ID {customerId}.";
        }

        return string.Join(
            Environment.NewLine,
            orders.Select(o =>
                $"Order ID: {o.Id}, " +
                $"Product: {o.Product}, " +
                $"Amount: ${o.Amount:F2}, " +
                $"Status: {o.Status}"));
    }
}