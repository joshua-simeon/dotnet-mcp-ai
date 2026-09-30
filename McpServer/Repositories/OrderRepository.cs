using McpServer.Data;
using McpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly CustomerDbContext _db;

    public OrderRepository(CustomerDbContext db)
    {
        _db = db;
    }

    public Order? GetOrder(int orderId)
    {
        return _db.Orders
            .AsNoTracking()
            .FirstOrDefault(o => o.Id == orderId);
    }

    public List<Order> GetOrdersByCustomerId(int customerId)
    {
        return _db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .ToList();
    }
}