using System.ComponentModel;
using ModelContextProtocol;
using McpServer.Services;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class OrderTools
{
    [McpServerTool(UseStructuredContent = true)]
    [Description("Gets order information by order ID.")]
    public static OrderResult GetOrder(
        OrderService orderService,
        [Description("The unique ID of the order.")] int orderId)
    {
        var order = orderService.GetOrder(orderId);

        if (order == null)
        {
            throw new McpException($"Order ID {orderId} was not found.");
        }

        return new OrderResult(
            order.Id,
            order.CustomerId,
            order.Product,
            order.Amount,
            order.Status);
    }
    [McpServerTool(UseStructuredContent = true)]
    [Description("Gets all orders for a specific customer.")]
    public static OrderResult[] GetOrdersByCustomer(
    OrderService orderService,
    [Description("The unique ID of the customer.")] int customerId)
    {
        var orders = orderService.GetOrdersByCustomerId(customerId);

        if (orders.Count == 0)
        {
            throw new McpException($"No orders were found for customer ID {customerId}.");
        }

        return orders
            .Select(order => new OrderResult(
                order.Id,
                order.CustomerId,
                order.Product,
                order.Amount,
                order.Status))
            .ToArray();
    }
}
