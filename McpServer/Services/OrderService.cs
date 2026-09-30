using McpServer.Models;
using McpServer.Repositories;

namespace McpServer.Services;

public class OrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public Order? GetOrder(int orderId)
    {
        return _repository.GetOrder(orderId);
    }

    public List<Order> GetOrdersByCustomerId(int customerId)
    {
        return _repository.GetOrdersByCustomerId(customerId);
    }
}