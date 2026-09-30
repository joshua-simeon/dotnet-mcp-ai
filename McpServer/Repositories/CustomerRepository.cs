using Microsoft.EntityFrameworkCore;
using McpServer.Data;
using McpServer.Models;

namespace McpServer.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly CustomerDbContext _db;

    public CustomerRepository(CustomerDbContext db)
    {
        _db = db;
    }

    public Customer? GetCustomer(int customerId)
    {
        return _db.Customers
            .AsNoTracking()
            .FirstOrDefault(c => c.Id == customerId);
    }

    public IReadOnlyList<Customer> SearchCustomersByName(string name)
    {
        return _db.Customers
            .AsNoTracking()
            .Where(c => c.Name.Contains(name))
            .ToList();
    }
}