using McpServer.Models;

namespace McpServer.Repositories;

public interface IOrderRepository
{
    Order? GetOrder(int orderId);

    List<Order> GetOrdersByCustomerId(int customerId);
}